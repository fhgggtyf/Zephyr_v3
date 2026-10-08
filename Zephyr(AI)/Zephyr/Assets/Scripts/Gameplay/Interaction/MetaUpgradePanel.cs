using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zephyr.Core.Localization;
using Zephyr.Core.Meta;
using Zephyr.Core.Save;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>Runtime controller for the replaceable MetaHub upgrade panel prefab.</summary>
    public sealed class MetaUpgradePanel : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Text _currency;
        [SerializeField] private Text _budgetValue;
        [SerializeField] private Text _allocatableValue;
        [SerializeField] private Text _budgetDescription;
        [SerializeField] private Text _allocatableDescription;
        [SerializeField] private Text _budgetButtonLabel;
        [SerializeField] private Text _allocatableButtonLabel;
        [SerializeField] private Button _budgetButton;
        [SerializeField] private Button _allocatableButton;
        [SerializeField] private Button _closeButton;
        private MetaUpgradeStation _owner;

        private void Awake()
        {
            ResolveReferences();
            _budgetButton?.onClick.AddListener(PurchaseBudget);
            _allocatableButton?.onClick.AddListener(PurchaseAllocatable);
            _closeButton?.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            _budgetButton?.onClick.RemoveListener(PurchaseBudget);
            _allocatableButton?.onClick.RemoveListener(PurchaseAllocatable);
            _closeButton?.onClick.RemoveListener(Close);
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Show(MetaUpgradeStation owner)
        {
            _owner = owner;
            ResolveReferences();
            Time.timeScale = 0f;
            Refresh();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            Time.timeScale = 1f;
            _owner?.NotifyPanelClosed(this);
            _owner = null;
            if (gameObject != null) Destroy(gameObject);
        }

        public void CloseWithoutNotifyingOwner()
        {
            Time.timeScale = 1f;
            _owner = null;
            if (gameObject != null) Destroy(gameObject);
        }

        private void PurchaseBudget() => Purchase(MetaUpgradeTarget.PotentialBudget);
        private void PurchaseAllocatable() => Purchase(MetaUpgradeTarget.PotentialAllocatable);

        private void Purchase(MetaUpgradeTarget target)
        {
            MetaData meta = SaveSystem.Instance?.Meta;
            if (meta == null) return;
            MetaUpgradeSO[] upgrades = _owner?.Upgrades;
            for (int i = 0; upgrades != null && i < upgrades.Length; i++)
            {
                if (upgrades[i].Target != target) continue;
                MetaUpgradeSystem.Purchase(upgrades[i], meta);
                Refresh();
                return;
            }
        }

        private void Refresh()
        {
            MetaData meta = SaveSystem.Instance?.Meta;
            if (meta == null) return;
            int budget = MetaUpgradeSystem.GetPotentialBudget(meta);
            int allocatable = MetaUpgradeSystem.GetPotentialAllocatable(meta);
            if (_title != null) _title.text = Localize("meta.upgrades.title", "Potential Upgrades");
            if (_currency != null) _currency.text = Localize("meta.upgrades.currency", "Meta Coins: {0}", meta.MetaCurrency);
            if (_budgetValue != null) _budgetValue.text = $"{budget}/{MetaUpgradeSystem.MaxPotentialBudget}";
            if (_allocatableValue != null) _allocatableValue.text = $"{allocatable}/{MetaUpgradeSystem.MaxPotentialAllocatable}";
            if (_budgetDescription != null) _budgetDescription.text = Localize("meta.upgrades.budget.description", "+2 Potential budget");
            if (_allocatableDescription != null) _allocatableDescription.text = Localize("meta.upgrades.allocatable.description", "+1 player-allocatable Potential");
            if (_budgetButtonLabel != null) _budgetButtonLabel.text = Localize("meta.upgrades.budget.buy", "Buy (+2) - 0");
            if (_allocatableButtonLabel != null) _allocatableButtonLabel.text = Localize("meta.upgrades.allocatable.buy", "Buy (+1) - 0");
            if (_budgetButton != null) _budgetButton.interactable = MetaUpgradeSystem.CanPurchaseTarget(MetaUpgradeTarget.PotentialBudget, meta);
            if (_allocatableButton != null) _allocatableButton.interactable = MetaUpgradeSystem.CanPurchaseTarget(MetaUpgradeTarget.PotentialAllocatable, meta);
        }

        private void ResolveReferences()
        {
            _title ??= FindText("Panel/Title");
            _currency ??= FindText("Panel/Currency");
            _budgetValue ??= FindText("Panel/BudgetRow/Value");
            _allocatableValue ??= FindText("Panel/AllocatableRow/Value");
            _budgetDescription ??= FindText("Panel/BudgetRow/Description");
            _allocatableDescription ??= FindText("Panel/AllocatableRow/Description");
            _budgetButtonLabel ??= FindText("Panel/BudgetRow/BuyButton/Label");
            _allocatableButtonLabel ??= FindText("Panel/AllocatableRow/BuyButton/Label");
            _budgetButton ??= transform.Find("Panel/BudgetRow/BuyButton")?.GetComponent<Button>();
            _allocatableButton ??= transform.Find("Panel/AllocatableRow/BuyButton")?.GetComponent<Button>();
            _closeButton ??= transform.Find("Panel/CloseButton")?.GetComponent<Button>();
        }

        private Text FindText(string path) => transform.Find(path)?.GetComponent<Text>();

        private static string Localize(string key, string fallback, params object[] args)
        {
            string value = LocalizationManager.Instance?.Get(key, args);
            return string.IsNullOrEmpty(value) || value == key ? string.Format(fallback, args) : value;
        }
    }
}
