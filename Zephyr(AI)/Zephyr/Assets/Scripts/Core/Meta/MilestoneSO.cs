using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Zephyr.Core.Meta
{
    public enum MilestoneCategory
    {
        Tutorial,
        Quest,
        AreaUnlock,
        Boss,
        Arc,
        Ending,
        WorldState
    }

    [CreateAssetMenu(fileName = "Milestone", menuName = "Zephyr/Progression/Milestone")]
    public sealed class MilestoneSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private LocalizedString _displayName;
        [SerializeField] private LocalizedString _description;
        [SerializeField] private MilestoneCategory _category;
        [SerializeField] private MilestoneConditionSO _completionCondition;
        [SerializeField] private List<MilestoneEffectSO> _effects = new List<MilestoneEffectSO>();
        [SerializeField] private bool _hiddenFromPlayer;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public LocalizedString DisplayName => _displayName;
        public LocalizedString Description => _description;
        public MilestoneCategory Category => _category;
        public MilestoneConditionSO CompletionCondition => _completionCondition;
        public IReadOnlyList<MilestoneEffectSO> Effects => _effects;
        public bool HiddenFromPlayer => _hiddenFromPlayer;
    }
}
