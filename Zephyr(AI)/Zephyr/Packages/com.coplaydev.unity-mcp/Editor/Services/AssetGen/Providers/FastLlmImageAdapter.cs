/*
 * FastLlmImageAdapter.cs
 * ----------------------
 * Module:  Editor / Services / AssetGen / Providers
 * Purpose: Custom image provider adapter for fastllm.top (an OpenAI-compatible relay).
 *          Three call paths, all synchronous (result arrives in the submit response):
 *
 *          TEXT -> IMAGE
 *            POST /v1/images/generations   (OpenAI Images API; documented for
 *            gpt-image-2.5-sunburst: prompt / size / n / response_format)
 *
 *          IMAGE -> IMAGE (reference-sprite based, e.g. "same character, attacking pose")
 *            Route B (primary): POST /v1/chat/completions  multimodal content array,
 *                     image_url part carrying the reference as data URI / http URL,
 *                     modalities ["image","text"] — mirrors OpenRouterAdapter.
 *            Route A (fallback): POST /v1/images/edits  multipart/form-data with the
 *                     reference file + prompt (OpenAI image-edit API).
 *            Both routes are attempted because NewAPI-style relays differ in how a
 *            backend image model is exposed; the combined error message names the route
 *            that rejected the call so support/capability gaps are easy to diagnose.
 *
 *          Inline base64 is preferred on the JSON routes so the job manager skips the
 *          download step; hosted URLs are also accepted (the relay documents
 *          response_format=url only, so /images/edits intentionally omits it and lets
 *          the job manager download the returned URL).
 *          [CUSTOM] Added by the project (embedded-package fork) — re-apply on upstream upgrade.
 * Dependencies: IImageProviderAdapter, ImageGenRequest, ProviderPollResult, IHttpTransport,
 *               ProviderHttp/LocalImage (internal, same asmdef), SecretRedactor, Newtonsoft.Json.
 * Ch.Ref:   Custom provider integration; text route = OpenAI Images API,
 *           chat route mirrors OpenRouterAdapter, edits route follows OpenAI edits multipart.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MCPForUnity.Editor.Security;
using MCPForUnity.Editor.Services.AssetGen.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Services.AssetGen.Providers
{
    /// <summary>
    /// fastllm.top image provider. One adapter instance handles a single job (the job manager
    /// captures it for submit+poll).
    /// </summary>
    public sealed class FastLlmImageAdapter : IImageProviderAdapter
    {
        // The only host credentials are ever sent to. ProviderHttp.RequireHost pins every
        // auth-bearing request to this exact host.
        private const string AllowedHost = "fastllm.top";
        private const string ImagesGenerationsEndpoint = "https://fastllm.top/v1/images/generations";
        private const string ImagesEditsEndpoint = "https://fastllm.top/v1/images/edits";
        private const string ChatCompletionsEndpoint = "https://fastllm.top/v1/chat/completions";

        // internal so the model catalog references it directly (single source of truth).
        internal const string DefaultModel = "gpt-image-2.5-sunburst";

        // The relay's documented response_format enum for this model is { url } — b64_json may or
        // may not be supported. We still request it on the JSON generation route because getting
        // inline bytes saves a CDN round trip; the response parser accepts either form.
        private const string Crlf = "\r\n";

        public string Id => "fastllm";

        private byte[] _inlineData;
        private string _downloadUrl;
        private string _error;

        public async Task<string> SubmitAsync(ImageGenRequest req, string apiKey, IHttpTransport http, CancellationToken ct)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (http == null) throw new ArgumentNullException(nameof(http));

            bool imageMode = string.Equals(req.Mode, "image", StringComparison.OrdinalIgnoreCase)
                             && (!string.IsNullOrEmpty(req.ImageUrl) || !string.IsNullOrEmpty(req.ImagePath));

            if (!imageMode)
            {
                await SubmitTextToImageAsync(req, apiKey, http, ct);
            }
            else
            {
                await SubmitImageToImageAsync(req, apiKey, http, ct);
            }
            return "ready";
        }

        public Task<ProviderPollResult> PollAsync(string providerJobId, string apiKey, IHttpTransport http, CancellationToken ct)
        {
            // All routes are synchronous: the work finished inside SubmitAsync.
            var result = new ProviderPollResult { Progress = 1f };
            if (!string.IsNullOrEmpty(_error) || (_inlineData == null && string.IsNullOrEmpty(_downloadUrl)))
            {
                result.State = ProviderPollState.Failed;
                result.Error = _error ?? "fastllm produced no image.";
            }
            else
            {
                result.State = ProviderPollState.Succeeded;
                result.InlineData = _inlineData; // takes precedence over DownloadUrl in the job manager
                result.DownloadUrl = _downloadUrl;
            }
            return Task.FromResult(result);
        }

        // ---------- text -> image: POST /v1/images/generations ----------

        private async Task SubmitTextToImageAsync(ImageGenRequest req, string apiKey, IHttpTransport http, CancellationToken ct)
        {
            string model = string.IsNullOrEmpty(req.Model) ? DefaultModel : req.Model;

            var body = new JObject
            {
                ["model"] = model,
                ["prompt"] = req.Prompt ?? string.Empty,
                ["n"] = 1,
                ["response_format"] = "b64_json",
            };

            // The relay accepts a fixed enum of sizes; map arbitrary width/height onto the
            // closest supported aspect ratio. Omit entirely when the caller didn't specify one
            // so the API applies its own default (1024x1024).
            string size = MapToSupportedSize(req.Width, req.Height);
            if (!string.IsNullOrEmpty(size))
                body["size"] = size;

            ProviderHttp.RequireHost(ImagesGenerationsEndpoint, AllowedHost, apiKey, "fastllm generations");

            var spec = new HttpRequestSpec
            {
                Method = "POST",
                Url = ImagesGenerationsEndpoint,
                ContentType = "application/json",
                Body = Encoding.UTF8.GetBytes(body.ToString(Formatting.None))
            };
            spec.Headers["Authorization"] = "Bearer " + apiKey;

            HttpResult res = await http.SendAsync(spec, ct);
            ResponseInfo info = ReadResponse(res, apiKey);
            if (!info.Ok)
            {
                _error = $"images/generations failed (status={info.Status}): {info.Detail}";
                return;
            }

            string imageRef = ExtractImagesApiImage(info.Json);
            IngestImageRef(imageRef, "images/generations returned no image (expected data[0].b64_json or data[0].url).");
        }

        // ---------- image -> image: chat multimodal, then images/edits fallback ----------

        private async Task SubmitImageToImageAsync(ImageGenRequest req, string apiKey, IHttpTransport http, CancellationToken ct)
        {
            string model = string.IsNullOrEmpty(req.Model) ? DefaultModel : req.Model;

            // image_url.url accepts a hosted URL or an inline base64 data URI (local image_path).
            string dataRef = !string.IsNullOrEmpty(req.ImageUrl)
                ? req.ImageUrl
                : LocalImage.ToDataUri(req.ImagePath);

            // ---- Route B: chat/completions multimodal (OpenRouter-style) ----
            var chatBody = new JObject
            {
                ["model"] = model,
                ["modalities"] = new JArray("image", "text"),
                ["messages"] = new JArray(new JObject
                {
                    ["role"] = "user",
                    ["content"] = new JArray(
                        new JObject { ["type"] = "text", ["text"] = req.Prompt ?? string.Empty },
                        new JObject { ["type"] = "image_url", ["image_url"] = new JObject { ["url"] = dataRef } })
                })
            };

            ProviderHttp.RequireHost(ChatCompletionsEndpoint, AllowedHost, apiKey, "fastllm chat");
            var chatSpec = new HttpRequestSpec
            {
                Method = "POST",
                Url = ChatCompletionsEndpoint,
                ContentType = "application/json",
                Body = Encoding.UTF8.GetBytes(chatBody.ToString(Formatting.None))
            };
            chatSpec.Headers["Authorization"] = "Bearer " + apiKey;

            HttpResult chatRes = await http.SendAsync(chatSpec, ct);
            ResponseInfo chatInfo = ReadResponse(chatRes, apiKey);

            string chatImageRef = chatInfo.Ok ? ExtractChatImage(chatInfo.Json) : null;
            if (chatInfo.Ok && !string.IsNullOrEmpty(chatImageRef))
            {
                IngestImageRef(chatImageRef, "chat/completions returned an unrecognized image payload.");
                return;
            }

            // Soft-fail diagnostics for route B: an HTTP error, or HTTP 200 with no image (often a
            // text-only refusal / "model does not support image output").
            string chatFailure = chatInfo.Ok
                ? "200 but no image in the reply: " + ProviderHttp.Truncate(ExtractChatText(chatInfo.Json))
                : $"status={chatInfo.Status}: {chatInfo.Detail}";

            // ---- Route A: /v1/images/edits multipart (local files only) ----
            // The OpenAI edits API takes a raw file part, so a remote image_url would have to be
            // downloaded first; the sprite workflow uses local Assets files via image_path.
            if (string.IsNullOrEmpty(req.ImagePath))
            {
                _error = "fastllm image->image failed on every available route.\n"
                       + $"  chat/completions: {chatFailure}\n"
                       + "  images/edits: skipped (fallback needs a local 'image_path' under Assets/).";
                return;
            }

            string editsFailure = await TryImagesEditsAsync(req, model, apiKey, http, ct);
            if (string.IsNullOrEmpty(editsFailure))
                return; // success: _inlineData/_downloadUrl populated

            _error = "fastllm image->image failed on both routes.\n"
                   + $"  chat/completions: {chatFailure}\n"
                   + $"  images/edits: {editsFailure}";
        }

        /// <summary>
        /// POST /v1/images/edits as multipart/form-data. Returns null on success (result stashed
        /// in _inlineData/_downloadUrl), or a redacted failure description.
        /// </summary>
        private async Task<string> TryImagesEditsAsync(ImageGenRequest req, string model, string apiKey, IHttpTransport http, CancellationToken ct)
        {
            byte[] fileBytes;
            try { fileBytes = File.ReadAllBytes(req.ImagePath); }
            catch (Exception ex) { return $"could not read reference image: {ex.GetType().Name}: {ex.Message}"; }

            string fileExt = Path.GetExtension(req.ImagePath) ?? ".png";
            string fileMime = MimeFromExtension(fileExt);

            var fields = new Dictionary<string, string>
            {
                ["model"] = model,
                ["prompt"] = req.Prompt ?? string.Empty,
                ["n"] = "1",
            };
            // Size is an optional fixed enum on edits too; omit when no dimensions were given.
            string size = MapToSupportedSize(req.Width, req.Height);
            if (!string.IsNullOrEmpty(size))
                fields["size"] = size;

            string boundary = "----MCPForUnityFastLlm" + Guid.NewGuid().ToString("N");
            byte[] multipartBody = BuildMultipartBody(boundary, fields, "image", "reference" + fileExt, fileMime, fileBytes);

            ProviderHttp.RequireHost(ImagesEditsEndpoint, AllowedHost, apiKey, "fastllm edits");
            var spec = new HttpRequestSpec
            {
                Method = "POST",
                Url = ImagesEditsEndpoint,
                ContentType = "multipart/form-data; boundary=" + boundary,
                Body = multipartBody
            };
            spec.Headers["Authorization"] = "Bearer " + apiKey;

            HttpResult res = await http.SendAsync(spec, ct);
            ResponseInfo info = ReadResponse(res, apiKey);
            if (!info.Ok)
                return $"status={info.Status}: {info.Detail}";

            string imageRef = ExtractImagesApiImage(info.Json);
            if (string.IsNullOrEmpty(imageRef))
                return "200 but no image (expected data[0].b64_json or data[0].url): " + ProviderHttp.Truncate(info.RawText);

            IngestImageRef(imageRef, null);
            return !string.IsNullOrEmpty(_error) ? _error : null;
        }

        // ---------- shared response / payload helpers ----------

        /// <summary>
        /// Accept an image reference that is either a base64 data URI (stored as inline bytes) or
        /// an http(s) URL (handed to the job manager for download). On failure sets _error.
        /// </summary>
        private void IngestImageRef(string imageRef, string missingMessage)
        {
            if (string.IsNullOrEmpty(imageRef))
            {
                _error = missingMessage ?? "fastllm returned no image.";
                return;
            }

            if (imageRef.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                int comma = imageRef.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                if (comma < 0)
                {
                    _error = "fastllm returned an unrecognized inline image payload.";
                    return;
                }
                try { _inlineData = Convert.FromBase64String(imageRef.Substring(comma + "base64,".Length)); }
                catch { _error = "fastllm image was not valid base64."; }
            }
            else
            {
                _downloadUrl = imageRef;
            }
        }

        /// <summary>OpenAI Images API shape: { data: [ { b64_json | url } ] } (generations + edits).</summary>
        private static string ExtractImagesApiImage(JObject json)
        {
            return json?["data"]?[0]?["b64_json"]?.ToString()
                   ?? json?["data"]?[0]?["url"]?.ToString();
        }

        /// <summary>
        /// Extract an image reference from a chat/completions reply. Handles the shapes emitted by
        /// OpenAI-compatible relays: message.images[], a content array with image_url parts, and
        /// Responses-style output_image parts with inline source data.
        /// </summary>
        private static string ExtractChatImage(JObject json)
        {
            JToken message = json?["choices"]?[0]?["message"];
            if (message == null) return null;

            // OpenRouter style: message.images[].image_url.url (or images[].url)
            if (message["images"] is JArray imgs && imgs.Count > 0)
            {
                string u = imgs[0]?["image_url"]?["url"]?.ToString() ?? imgs[0]?["url"]?.ToString();
                if (!string.IsNullOrEmpty(u)) return u;
            }

            // Content-array styles
            if (message["content"] is JArray parts)
            {
                foreach (JToken part in parts)
                {
                    string u = part?["image_url"]?["url"]?.ToString();
                    if (!string.IsNullOrEmpty(u)) return u;

                    // OpenAI Responses-style: { type:"output_image", source:{ data:"<base64>" } }
                    if (string.Equals(part?["type"]?.ToString(), "output_image", StringComparison.OrdinalIgnoreCase))
                    {
                        string b64 = part?["source"]?["data"]?.ToString();
                        if (!string.IsNullOrEmpty(b64)) return "data:image/png;base64," + b64;
                    }
                }
            }
            return null;
        }

        /// <summary>Best-effort plain-text extraction from a chat reply, used for failure diagnostics.</summary>
        private static string ExtractChatText(JObject json)
        {
            JToken message = json?["choices"]?[0]?["message"];
            if (message?["content"] is JArray parts)
            {
                var sb = new StringBuilder();
                foreach (JToken part in parts)
                {
                    if (string.Equals(part?["type"]?.ToString(), "text", StringComparison.OrdinalIgnoreCase))
                        sb.Append(part?["text"]?.ToString());
                }
                return sb.ToString();
            }
            return message?["content"]?.ToString();
        }

        private static ResponseInfo ReadResponse(HttpResult res, string apiKey)
        {
            var info = new ResponseInfo
            {
                Ok = res?.Ok == true,
                Status = res?.Status,
                RawText = ProviderHttp.BodyText(res)
            };

            if (!string.IsNullOrEmpty(info.RawText))
            {
                try { info.Json = JObject.Parse(info.RawText); } catch { /* non-JSON body */ }
            }

            if (!info.Ok)
            {
                // OpenAI-style: { error: { message } } or a bare { error: "..." } / { detail: "..." }.
                info.Detail = info.Json?["error"]?["message"]?.ToString()
                              ?? info.Json?["error"]?.ToString()
                              ?? info.Json?["detail"]?.ToString()
                              ?? ProviderHttp.Truncate(info.RawText);
                info.Detail = SecretRedactor.Scrub(info.Detail, apiKey);
            }
            return info;
        }

        /// <summary>
        /// Build a multipart/form-data body with simple string fields followed by one file field.
        /// Hand-rolled because the package's HTTP abstraction takes raw bytes (and there is no
        /// System.Net.Http dependency in the editor asmdef).
        /// </summary>
        private static byte[] BuildMultipartBody(
            string boundary,
            IDictionary<string, string> fields,
            string fileFieldName,
            string fileName,
            string fileMime,
            byte[] fileBytes)
        {
            using (var ms = new MemoryStream())
            {
                foreach (KeyValuePair<string, string> field in fields)
                {
                    WriteAscii(ms, "--" + boundary + Crlf);
                    WriteAscii(ms, $"Content-Disposition: form-data; name=\"{field.Key}\"" + Crlf + Crlf);
                    WriteAscii(ms, field.Value + Crlf);
                }

                WriteAscii(ms, "--" + boundary + Crlf);
                WriteAscii(ms,
                    $"Content-Disposition: form-data; name=\"{fileFieldName}\"; filename=\"{fileName}\"" + Crlf);
                WriteAscii(ms, "Content-Type: " + fileMime + Crlf + Crlf);
                ms.Write(fileBytes, 0, fileBytes.Length);
                WriteAscii(ms, Crlf + "--" + boundary + "--" + Crlf);

                return ms.ToArray();
            }
        }

        private static void WriteAscii(Stream s, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            s.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// gpt-image-style endpoints only accept a small size enum. Map the requested pixel
        /// dimensions onto the closest supported aspect ratio. Returns null when no explicit
        /// dimensions were supplied (caller omits the field, server applies its default).
        /// </summary>
        private static string MapToSupportedSize(int width, int height)
        {
            if (width <= 0 || height <= 0) return null;

            float ratio = (float)width / height;
            if (ratio > 1.15f) return "1536x1024"; // landscape
            if (ratio < 0.87f) return "1024x1536"; // portrait
            return "1024x1024";                    // square
        }

        private static string MimeFromExtension(string ext)
        {
            switch ((ext ?? string.Empty).ToLowerInvariant())
            {
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".webp": return "image/webp";
                case ".gif": return "image/gif";
                default: return "application/octet-stream";
            }
        }

        private sealed class ResponseInfo
        {
            public bool Ok;
            public int? Status;
            public string RawText;
            public JObject Json;
            public string Detail;
        }
    }
}
