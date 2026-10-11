using UnityEngine;
using System.Collections.Generic;
using Zephyr.Core.Runtime.Lifecycle;

namespace Zephyr.Core.Audio
{
    public enum AudioBus { SFX, Music, UI }

    [CreateAssetMenu(menuName = "Zephyr/Audio/Event")]
    public sealed class AudioEventSO : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips;
        [SerializeField] private AudioBus _bus = AudioBus.SFX;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField] private Vector2 _pitchRange = Vector2.one;
        [SerializeField] private bool _loop;
        [SerializeField] private int _priority;

        public AudioClip[] Clips => _clips;
        public AudioBus Bus => _bus;
        public float Volume => _volume;
        public Vector2 PitchRange => _pitchRange;
        public bool Loop => _loop;
        public int Priority => _priority;
    }

    /// <summary>Persistent audio owner. Playback assets are supplied by upper-layer events.</summary>
    [SingletonLifetime(SingletonLifetime.Persistent, "Persistent")]
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;

        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _uiVolume = 1f;
        [SerializeField, Min(1)] private int _voiceCount = 16;
        private readonly bool[] _muted = new bool[3];
        private readonly Dictionary<AudioEventSO, AudioSource> _playing = new Dictionary<AudioEventSO, AudioSource>();
        private readonly List<AudioSource> _voices = new List<AudioSource>();
        private readonly Dictionary<AudioSource, int> _priorities = new Dictionary<AudioSource, int>();

        public float GetBusVolume(AudioBus bus) => bus switch
        {
            AudioBus.SFX => _sfxVolume,
            AudioBus.Music => _musicVolume,
            _ => _uiVolume
        };

        private void Awake()
        {
            if (!SingletonLifecycle.TryClaim(this, ref _instance, nameof(AudioManager)))
            {
                Destroy(gameObject);
                return;
            }
            for (int i = 0; i < _voiceCount; i++) CreateVoice();
        }

        private void OnDestroy() => SingletonLifecycle.Release(this, ref _instance);

        public void SetBusVolume(AudioBus bus, float value)
        {
            value = Mathf.Clamp01(value);
            switch (bus)
            {
                case AudioBus.SFX: _sfxVolume = value; break;
                case AudioBus.Music: _musicVolume = value; break;
                case AudioBus.UI: _uiVolume = value; break;
            }
        }

        public void MuteBus(AudioBus bus, bool muted) => _muted[(int)bus] = muted;
        public bool IsMuted(AudioBus bus) => _muted[(int)bus];

        public AudioSource Play(AudioEventSO audioEvent, Vector2? worldPosition = null)
        {
            if (audioEvent == null || audioEvent.Clips == null || audioEvent.Clips.Length == 0 || IsMuted(audioEvent.Bus)) return null;
            AudioSource source = GetVoice(audioEvent.Priority);
            source.clip = audioEvent.Clips[Random.Range(0, audioEvent.Clips.Length)];
            source.volume = audioEvent.Volume * GetBusVolume(audioEvent.Bus);
            source.pitch = Random.Range(audioEvent.PitchRange.x, Mathf.Max(audioEvent.PitchRange.x, audioEvent.PitchRange.y));
            source.loop = audioEvent.Loop;
            source.priority = Mathf.Clamp(256 - audioEvent.Priority, 0, 256);
            _priorities[source] = audioEvent.Priority;
            source.spatialBlend = worldPosition.HasValue ? 1f : 0f;
            if (worldPosition.HasValue) source.transform.position = worldPosition.Value;
            source.Play();
            if (audioEvent.Loop) _playing[audioEvent] = source;
            return source;
        }

        public void Stop(AudioEventSO audioEvent)
        {
            if (audioEvent == null || !_playing.TryGetValue(audioEvent, out var source)) return;
            source.Stop();
            _playing.Remove(audioEvent);
        }

        private AudioSource GetVoice(int priority)
        {
            for (int i = 0; i < _voices.Count; i++) if (!_voices[i].isPlaying) return _voices[i];
            AudioSource eviction = _voices[0];
            for (int i = 1; i < _voices.Count; i++)
                if (_priorities[_voices[i]] < _priorities[eviction]) eviction = _voices[i];
            eviction.Stop();
            _priorities[eviction] = priority;
            return eviction;
        }

        private AudioSource CreateVoice()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _voices.Add(source);
            _priorities[source] = 0;
            return source;
        }
    }
}
