using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zephyr.Core.Weapons;

namespace Zephyr.Gameplay.Player.UI
{
    public sealed class WeaponStatsTooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _name;
        [SerializeField] private Text _details;
        [SerializeField] private Canvas _canvas;

        private Camera _uiCamera;

        private void Awake()
        {
            if (_panel == null) _panel = transform as RectTransform;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            _uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            Hide();
        }

        private void Update()
        {
            if (gameObject.activeSelf) FollowPointer();
        }

        public void Show(WeaponSO weapon)
        {
            if (weapon == null)
            {
                Hide();
                return;
            }

            if (_icon != null) _icon.sprite = weapon.Icon;
            if (_name != null) _name.text = weapon.DisplayName;
            if (_details != null)
            {
                _details.text =
                    $"Damage {weapon.BaseFlatDamage:F0}  x{weapon.BasePercentDamage:F2}\n" +
                    $"Speed {weapon.BaseAttackSpeed:F2}\n" +
                    $"Crit {weapon.BaseCritChance:P0}  Crit Dmg {weapon.BaseCritMultiplier:F2}\n" +
                    $"Pen {weapon.BaseFlatPen:F0}  {weapon.BasePercentPen:P0}\n" +
                    $"Element {weapon.ElementalType}  {weapon.Category}";
            }

            gameObject.SetActive(true);
            FollowPointer();
        }

        public void Hide() => gameObject.SetActive(false);

        private void FollowPointer()
        {
            if (_panel == null || _canvas == null) return;
            RectTransform canvasRect = _canvas.transform as RectTransform;
            Vector2 pointerPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, pointerPosition, _uiCamera, out Vector2 localPoint)) return;

            Vector2 size = _panel.rect.size;
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 position = localPoint + new Vector2(size.x * 0.5f + 18f, -size.y * 0.5f - 18f);
            position.x = Mathf.Clamp(position.x, -canvasSize.x * 0.5f + size.x * 0.5f,
                canvasSize.x * 0.5f - size.x * 0.5f);
            position.y = Mathf.Clamp(position.y, -canvasSize.y * 0.5f + size.y * 0.5f,
                canvasSize.y * 0.5f - size.y * 0.5f);
            _panel.anchoredPosition = position;
        }
    }
}
