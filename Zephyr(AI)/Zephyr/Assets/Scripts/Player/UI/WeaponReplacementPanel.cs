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
        private Func<WeaponSlot, bool> _confirm;
        private Action _cancel;
        private bool _closing;

        private void Awake()
        {
            BindLocalization(_ => { });
            BuildInterface();
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
            GameplayTimePause.SetModalOpen(this, true);
            RefreshCard("Primary", primary);
            RefreshCard("Secondary", secondary);
            RefreshCard("Replacement", replacement);
            gameObject.SetActive(true);
        }

        private void BuildInterface()
        {
            RectTransform root = transform as RectTransform;
            if (root == null) return;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Image overlay = RuntimeUIFactory.CreateImage("Overlay", root,
                new Color(0f, 0f, 0f, 0.72f));
            RuntimeUIFactory.Stretch((RectTransform)overlay.transform);

            Text title = RuntimeUIFactory.CreateText("Title", root, 26, TextAnchor.MiddleCenter,
                Color.white);
            RuntimeUIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f),
                new Vector2(760f, 42f), new Vector2(0f, -42f));
            title.text = Localize("weapon.replacement.title", "Weapon Full - Choose a Weapon to Replace");

            CreateCard(root, "Primary", new Vector2(-300f, -8f), WeaponSlot.Primary, true);
            CreateCard(root, "Secondary", new Vector2(0f, -8f), WeaponSlot.Secondary, true);
            CreateCard(root, "Replacement", new Vector2(300f, -8f), WeaponSlot.Primary, false);

            Text hint = RuntimeUIFactory.CreateText("Hint", root, 15, TextAnchor.MiddleCenter,
                new Color(0.8f, 0.84f, 0.84f, 1f));
            RuntimeUIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f),
                new Vector2(820f, 30f), new Vector2(0f, 30f));
            hint.text = Localize("weapon.replacement.hint", "Choose the slot to replace. The old weapon drops at your feet.");

            Text cancelLabel;
            Button cancel = RuntimeUIFactory.CreateButton("Cancel", root,
                new Color(0.2f, 0.22f, 0.24f, 1f), Color.white, out cancelLabel);
            RuntimeUIFactory.Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(150f, 38f), new Vector2(0f, -20f));
            cancelLabel.text = Localize("common.cancel", "Cancel");
            cancel.onClick.AddListener(CloseCancelled);
        }

        private void CreateCard(Transform root, string id, Vector2 position, WeaponSlot slot,
            bool replaceable)
        {
            Image card = RuntimeUIFactory.CreateImage(id + "Card", root,
                replaceable ? new Color(0.08f, 0.11f, 0.13f, 0.97f)
                    : new Color(0.16f, 0.12f, 0.06f, 0.97f));
            RuntimeUIFactory.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f),
                new Vector2(235f, 285f), position);

            Text slotLabel = RuntimeUIFactory.CreateText("Slot", card.transform, 16,
                TextAnchor.MiddleCenter, new Color(0.75f, 0.82f, 0.82f, 1f));
            RuntimeUIFactory.Place(slotLabel.rectTransform, new Vector2(0.5f, 1f),
                new Vector2(215f, 28f), new Vector2(0f, -22f));
            slotLabel.text = replaceable
                ? Localize(id == "Primary" ? "weapon.replacement.primary" : "weapon.replacement.secondary", id)
                : Localize("weapon.replacement.new", "New Weapon");

            Image icon = RuntimeUIFactory.CreateImage("Icon", card.transform,
                new Color(0.24f, 0.27f, 0.28f, 1f));
            RuntimeUIFactory.Place((RectTransform)icon.transform, new Vector2(0.5f, 0.5f),
                new Vector2(76f, 76f), new Vector2(0f, 54f));

            Text name = RuntimeUIFactory.CreateText("Name", card.transform, 18,
                TextAnchor.MiddleCenter, Color.white);
            RuntimeUIFactory.Place(name.rectTransform, new Vector2(0.5f, 0.5f),
                new Vector2(215f, 28f), new Vector2(0f, 5f));

            Text details = RuntimeUIFactory.CreateText("Details", card.transform, 12,
                TextAnchor.UpperCenter, new Color(0.86f, 0.88f, 0.88f, 1f));
            RuntimeUIFactory.Place(details.rectTransform, new Vector2(0.5f, 0f),
                new Vector2(215f, 92f), new Vector2(0f, 74f));

            if (!replaceable) return;

            Text buttonText;
            Button button = RuntimeUIFactory.CreateButton("ReplaceButton", card.transform,
                new Color(0.24f, 0.4f, 0.34f, 1f), Color.white, out buttonText);
            RuntimeUIFactory.Place(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(190f, 38f), new Vector2(0f, 22f));
            buttonText.text = Localize("weapon.replacement.replace", "Replace");
            button.onClick.AddListener(() => Confirm(slot));
        }

        private void RefreshCard(string id, WeaponSO weapon)
        {
            Transform card = transform.Find(id + "Card");
            if (card == null) return;
            Image icon = card.Find("Icon")?.GetComponent<Image>();
            Text name = card.Find("Name")?.GetComponent<Text>();
            Text details = card.Find("Details")?.GetComponent<Text>();
            if (icon != null)
            {
                icon.sprite = weapon != null ? weapon.Icon : null;
                icon.color = weapon != null && weapon.Icon != null
                    ? Color.white
                    : new Color(0.24f, 0.27f, 0.28f, 1f);
            }
            if (name != null)
                name.text = weapon == null ? "-"
                    : !string.IsNullOrWhiteSpace(weapon.DisplayNameLocalizationKey)
                        ? Localize(weapon.DisplayNameLocalizationKey, weapon.DisplayName)
                        : weapon.DisplayName;
            if (details != null) details.text = FormatDetails(weapon);
        }

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

        private void OnDestroy()
        {
            GameplayTimePause.SetModalOpen(this, false);
            UnbindLocalization();
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
