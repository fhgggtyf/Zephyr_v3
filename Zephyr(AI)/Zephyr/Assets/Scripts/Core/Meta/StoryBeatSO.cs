using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.UI.Dialogue;

namespace Zephyr.Core.Meta
{
    [CreateAssetMenu(fileName = "StoryBeat", menuName = "Zephyr/Story/Story Beat")]
    public sealed class StoryBeatSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string[] _requiredObjectiveIds = Array.Empty<string>();
        [SerializeField] private DialogueSO _dialogue;
        [SerializeField] private int _priority;
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public IReadOnlyList<string> RequiredObjectiveIds => _requiredObjectiveIds;
        public DialogueSO Dialogue => _dialogue;
        public int Priority => _priority;
    }
}
