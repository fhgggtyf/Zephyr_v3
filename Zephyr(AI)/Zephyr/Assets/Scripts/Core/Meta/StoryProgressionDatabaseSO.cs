using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.Meta
{
    [CreateAssetMenu(fileName = "StoryProgressionDatabase", menuName = "Zephyr/Progression/Database")]
    public sealed class StoryProgressionDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<StoryFactSO> _facts = new List<StoryFactSO>();
        [SerializeField] private List<MilestoneSO> _milestones = new List<MilestoneSO>();

        public IReadOnlyList<StoryFactSO> Facts => _facts;
        public IReadOnlyList<MilestoneSO> Milestones => _milestones;
    }
}
