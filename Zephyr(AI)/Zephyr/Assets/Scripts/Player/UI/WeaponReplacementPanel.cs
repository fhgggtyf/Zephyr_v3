using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zephyr.Core.Localization;
using Zephyr.Core.UI;
using Zephyr.Core.Weapons;

namespace Zephyr.Gameplay.Player.UI
{
    public sealed class WeaponReplacementPanel : LocalizedBehaviour
    {
        [Serializable]
        private sealed class CardView
        {
            public Image Icon;
            public Text SlotLabel;
            public Text Name;
            public Text Details;
            public Button ReplaceButton;
            public Text ReplaceLabel;
        }

        [SerializeField] private Text _title;
        [SerializeField] private Text _hint;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Text _cancelLabel;
        [SerializeField] private CardView _primaryCard;
        [SerializeField] private CardView _secondaryCard;
        [SerializeField] private CardView _replacementCard;

        private Func<WeaponSlot, bool> _confirm;
        private Action _cancel;
        private bool _closing;

        private void Awake()
        {
            BindLocalization(HandleLocaleChanged);
            _cancelButton?.onClick.AddListener(CloseCancelled);
            _primaryCard?.ReplaceButton?.onClick.AddListener(ConfirmPrimary);
            _secondaryCard?.ReplaceButton?.onClick.AddListener(ConfirmSecondary);
            RefreshStaticText();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseCancelled();
        }

        public void Show(WeaponSO primary, WeaponSO secondary, WeaponSO replacement,
            Func<WeaponSlot, bool> confirm, Action cancel)
        {
            _confirm = confirm;
            _cancel = cancel;
            _closing = false;
            GameplayTimePause.SetModalOpen(this, true);
            RefreshStaticText();
            RefreshCard(_primaryCard, primary);
            RefreshCard(_secondaryCard, secondary);
            RefreshCard(_replacementCard, replacement);
            gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            _cancelButton?.onClick.RemoveListener(CloseCancelled);
            _primaryCard?.ReplaceButton?.onClick.RemoveListener(ConfirmPrimary);
            _secondaryCard?.ReplaceButton?.onClick.RemoveListener(ConfirmSecondary);
            GameplayTimePause.SetModalOpen(this, false);
            UnbindLocalization();
        }

        private void HandleLocaleChanged(LocaleId _)
        {
            RefreshStaticText();
        }

        private void RefreshStaticText()
        {
            if (_title != null)
                _title.text = Localize("weapon.replacement.title", "Weapon Full - Choose a Weapon to Replace");
            if (_hint != null)
                _hint.text = Localize("weapon.replacement.hint", "Choose the slot to replace. The old weapon drops at your feet.");
            if (_cancelLabel != null)
                _cancelLabel.text = Localize("common.cancel", "Cancel");

            if (_primaryCard != null)
            {
                if (_primaryCard.SlotLabel != null)
                    _primaryCard.SlotLabel.text = Localize("weapon.replacement.primary", "Primary");
                if (_primaryCard.ReplaceLabel != null)
                    _primaryCard.ReplaceLabel.text = Localize("weapon.replacement.replace", "Replace");
            }

            if (_secondaryCard != null)
            {
                if (_secondaryCard.SlotLabel != null)
                    _secondaryCard.SlotLabel.text = Localize("weapon.replacement.secondary", "Secondary");
                if (_secondaryCard.ReplaceLabel != null)
                    _secondaryCard.ReplaceLabel.text = Localize("weapon.replacement.replace", "Replace");
            }

            if (_replacementCard?.SlotLabel != null)
                _replacementCard.SlotLabel.text = Localize("weapon.replacement.new", "New Weapon");
        }

        private void RefreshCard(CardView card, WeaponSO weapon)
        {
            if (card == null) return;

            if (card.Icon != null)
            {
                card.Icon.sprite = weapon != null ? weapon.Icon : null;
                card.Icon.color = weapon != null && weapon.Icon != null
                    ? Color.white
                    : new Color(0.24f, 0.27f, 0.28f, 1f);
            }

            if (card.Name != null)
                card.Name.text = weapon == null ? "-"
                    : !string.IsNullOrWhiteSpace(weapon.DisplayNameLocalizationKey)
                        ? Localize(weapon.DisplayNameLocalizationKey, weapon.DisplayName)
                        : weapon.DisplayName;
            if (card.Details != null) card.Details.text = FormatDetails(weapon);
        }

        private void ConfirmPrimary() => Confirm(WeaponSlot.Primary);
        private void ConfirmSecondary() => Confirm(WeaponSlot.Secondary);

        private void Confirm(WeaponSlot slot)
        {
            if (_closing || _confirm == null || !_confirm(slot)) return;
            _closing = true;
            GameplayTimePause.SetModalOpen(this, false);
            Destroy(gameObject);
        }

        private void CloseCancelled()
        {
            if (_closing) return;
            _closing = true;
            GameplayTimePause.SetModalOpen(this, false);
            _cancel?.Invoke();
            Destroy(gameObject);
        }

        private string FormatDetails(WeaponSO weapon)
        {
            if (weapon == null) return "-";
            return Localize("weapon.replacement.stats",
                "Damage {0} x{1}\nSpeed {2}\nCrit {3}\nPen {4} / {5}\n{6} | {7}",
                weapon.BaseFlatDamage.ToString("F0"), weapon.BasePercentDamage.ToString("F2"),
                weapon.BaseAttackSpeed.ToString("F2"), weapon.BaseCritChance.ToString("P0"),
                weapon.BaseFlatPen.ToString("F0"), weapon.BasePercentPen.ToString("P0"),
                weapon.ElementalType, weapon.Category);
        }
    }
}
