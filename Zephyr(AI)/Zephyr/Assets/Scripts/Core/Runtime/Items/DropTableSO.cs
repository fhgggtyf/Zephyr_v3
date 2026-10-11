using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.Items
{
    [CreateAssetMenu(fileName = "DropTable", menuName = "Zephyr/Items/Drop Table")]
    public sealed class DropTableSO : ScriptableObject
    {
        [SerializeField] private DropEntry[] _entries = Array.Empty<DropEntry>();
        [Min(1)] [SerializeField] private int _rollCount = 1;
        public IReadOnlyList<DropEntry> Entries => _entries;
        public int RollCount => Mathf.Max(1, _rollCount);
    }
}
