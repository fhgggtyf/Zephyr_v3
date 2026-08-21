using UnityEngine;
using UnityEngine.UI;

namespace Zephyr.Core.UI
{
    public static class RuntimeUIFactory
    {
        private static Font _font;

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = 5;
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(string name, Transform parent, int fontSize,
            TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, Color background,
            Color foreground, out Text label)
        {
            Image image = CreateImage(name, parent, background);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(0.84f, 0.9f, 0.88f, 1f),
                pressedColor = new Color(0.62f, 0.72f, 0.68f, 1f),
                selectedColor = new Color(0.84f, 0.9f, 0.88f, 1f),
                disabledColor = new Color(0.32f, 0.32f, 0.32f, 0.7f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            label = CreateText("Label", image.transform, 22, TextAnchor.MiddleCenter, foreground);
            Stretch((RectTransform)label.transform, 12f, 6f);
            return button;
        }

        public static void Stretch(RectTransform rect, float horizontalPadding = 0f,
            float verticalPadding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = UnityEngine.Font.CreateDynamicFontFromOSFont(new[]
                    {
                        "Microsoft YaHei",
                        "Noto Sans CJK SC",
                        "PingFang SC",
                        "Arial Unicode MS",
                        "Arial"
                    }, 32);
                    if (_font == null)
                        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _font;
            }
        }
    }
}
