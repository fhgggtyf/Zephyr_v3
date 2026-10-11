using UnityEngine;
using Zephyr.Core.Contracts;
using Zephyr.Core.Interfaces;
using Zephyr.Core.ObjectPool;
using Zephyr.Core.Save;
using Zephyr.Core.Weapons;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Events;

namespace Zephyr.Core.Items
{
    [RequireComponent(typeof(PooledObject))]
    public sealed class WeaponPickup : MonoBehaviour, IInteractable, IInteractionFocusReceiver, IWeaponPickup
    {
        [SerializeField] private WeaponSO _weapon;
        [SerializeField] private SpriteRenderer _iconRenderer;
        [SerializeField] private SpriteRenderer _glowRenderer;
        [SerializeField] private Transform _visualRoot;
        [SerializeField, Min(0f)] private float _hoverAmplitude = 0.08f;
        [SerializeField, Min(0.01f)] private float _hoverFrequency = 3f;
        private PooledObject _pooledObject;
        private Vector3 _visualBaseLocalPosition;
        private bool _focused;
        private WeaponController _playerController;
        private bool _subscribedToPlayerTarget;
        private ISaveDataGateway _saveData;
        private static Sprite _fallbackSprite;

        public WeaponSO Weapon => _weapon;

        private void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
            _saveData = SaveSystem.Instance;
            EnsureVisuals();
            _visualBaseLocalPosition = _visualRoot != null ? _visualRoot.localPosition : Vector3.zero;
            RefreshVisuals();
        }

        private void OnEnable()
        {
            SubscribeToPlayerTarget();
            if (_visualRoot == null) EnsureVisuals();
            _visualBaseLocalPosition = _visualRoot != null ? _visualRoot.localPosition : Vector3.zero;
            RefreshVisuals();
        }

        private void Start()
        {
            SubscribeToPlayerTarget();
        }

        private void OnDisable()
        {
            if (_subscribedToPlayerTarget && EventBus.Instance != null)
                EventBus.Instance.Unsubscribe<PlayerTargetChangedEvent>(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = false;
            _playerController = null;
        }

        private void SubscribeToPlayerTarget()
        {
            if (EventBus.Instance == null) return;
            EventBus.Instance.Subscribe<PlayerTargetChangedEvent>(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = true;
            if (EventBus.Instance.TryGetLatest(out PlayerTargetChangedEvent current))
                HandlePlayerTargetChanged(current);
        }

        private void HandlePlayerTargetChanged(PlayerTargetChangedEvent change)
        {
            _playerController = change.Player != null
                ? change.Player.GetComponentInChildren<WeaponController>(true)
                : null;
        }

        private void Update()
        {
            if (!_focused || _visualRoot == null) return;
            Vector3 position = _visualBaseLocalPosition;
            position.y += Mathf.Sin(Time.time * _hoverFrequency) * _hoverAmplitude;
            _visualRoot.localPosition = position;
        }

        public void ResetState()
        {
            _focused = false;
            if (_visualRoot != null) _visualRoot.localPosition = _visualBaseLocalPosition;
        }

        public void Spawn(Vector2 position, WeaponSO weapon)
        {
            transform.position = position;
            _weapon = weapon;
            ResetState();
            RefreshVisuals();
        }

        public void Spawn(Vector2 position)
        {
            transform.position = position;
            ResetState();
            RefreshVisuals();
        }

        void IWeaponPickup.Spawn(Vector3 position) => Spawn((Vector2)position);

        public bool CanInteract() => _weapon != null && _playerController != null;

        public void OnInteract()
        {
            WeaponController controller = _playerController;
            if (controller == null || _weapon == null) return;
            controller.Equip(controller.ActiveSlot, _weapon);
            if (_saveData?.CurrentRun != null && int.TryParse(_weapon.WeaponId, out int id))
                _saveData.CurrentRun.AcquiredWeaponIds.Add(id);
            if (_pooledObject != null)
            {
                // Scene-authored pickups can carry PooledObject without being
                // initialized by an ObjectPool. Return to a pool when present,
                // then explicitly hide the object as the safe fallback.
                _pooledObject.ReturnToPool();
                if (gameObject.activeSelf) gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetInteractionFocused(bool focused)
        {
            _focused = focused;
            if (!focused && _visualRoot != null) _visualRoot.localPosition = _visualBaseLocalPosition;
        }

        private void EnsureVisuals()
        {
            if (_visualRoot == null) _visualRoot = transform;
            if (_iconRenderer == null) _iconRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_glowRenderer == null)
            {
                Transform glow = transform.Find("Glow");
                if (glow == null)
                {
                    var glowObject = new GameObject("Glow");
                    glow = glowObject.transform;
                    glow.SetParent(transform, false);
                    glow.localScale = Vector3.one * 1.35f;
                }
                _glowRenderer = glow.GetComponent<SpriteRenderer>();
                if (_glowRenderer == null) _glowRenderer = glow.gameObject.AddComponent<SpriteRenderer>();
                _glowRenderer.sortingOrder = -1;
            }
        }

        private void RefreshVisuals()
        {
            EnsureVisuals();
            if (_iconRenderer != null) _iconRenderer.sprite = _weapon != null ? _weapon.Icon : null;
            if (_glowRenderer == null) return;
            if (_glowRenderer.sprite == null) _glowRenderer.sprite = GetFallbackSprite();
            _glowRenderer.color = GetRarityColor(_weapon != null ? _weapon.Rarity : WeaponRarity.Common);
        }

        private static Sprite GetFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;
            _fallbackSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            _fallbackSprite.name = "RuntimeInteractionGlow";
            return _fallbackSprite;
        }

        private static Color GetRarityColor(WeaponRarity rarity)
        {
            switch (rarity)
            {
                case WeaponRarity.Uncommon: return new Color(0.25f, 1f, 0.25f, 0.5f);
                case WeaponRarity.Rare: return new Color(0.75f, 0.25f, 1f, 0.55f);
                case WeaponRarity.Epic: return new Color(1f, 0.75f, 0.1f, 0.6f);
                case WeaponRarity.Legendary: return new Color(1f, 0.15f, 0.1f, 0.65f);
                default: return new Color(1f, 1f, 1f, 0.45f);
            }
        }
    }
}
