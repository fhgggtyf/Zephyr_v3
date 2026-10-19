using System.Collections.Generic;
using UnityEngine;
using Zephyr.Contracts.Flow;
using Zephyr.Core.Stats;

namespace Zephyr.Core.Flow
{
    [CreateAssetMenu(menuName = "Zephyr/Flow/Scene")]
    public sealed class SceneSO : ScriptableObject, IStatModifierSource, ISceneRuleSource
    {
        [SerializeField] private SceneId _id;
        [SerializeField] private Object _sceneAsset;
        [SerializeField, HideInInspector] private string _sceneName;
        [SerializeField, HideInInspector] private string _scenePath;
        [Header("Scene Stat Modifiers")]
        [SerializeField] private List<StatModifierDefinition> _statModifiers = new List<StatModifierDefinition>();
        [Header("Scene Rule Overrides")]
        [SerializeField] private List<SceneRuleOverrideDefinition> _sceneRuleOverrides = new List<SceneRuleOverrideDefinition>();

        public SceneId Id => _id;
        public string SceneName => _sceneName;
        public string ScenePath => _scenePath;
        public string LoadKey => string.IsNullOrWhiteSpace(_scenePath) ? _sceneName : _scenePath;
        public IReadOnlyList<StatModifierDefinition> Modifiers => _statModifiers;
        public IReadOnlyList<StatModifierDefinition> StatModifiers => _statModifiers;
        public IReadOnlyList<SceneRuleOverrideDefinition> RuleOverrides => _sceneRuleOverrides;
        public bool IsValid => !string.IsNullOrWhiteSpace(_sceneName) &&
                               !string.IsNullOrWhiteSpace(_scenePath) &&
                               _scenePath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase);
        public bool AppliesTo(IStatModifierTarget target) => target != null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_sceneAsset == null)
            {
                _sceneName = string.Empty;
                _scenePath = string.Empty;
                return;
            }

            _scenePath = UnityEditor.AssetDatabase.GetAssetPath(_sceneAsset);
            if (!_scenePath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"SceneSO '{name}' must reference a Unity scene asset.", this);
                _sceneName = string.Empty;
                _scenePath = string.Empty;
                return;
            }

            _sceneName = System.IO.Path.GetFileNameWithoutExtension(_scenePath);

            if (_sceneRuleOverrides == null) return;
            var seenRules = new HashSet<SceneRuleOverrideKind>();
            for (int i = 0; i < _sceneRuleOverrides.Count; i++)
            {
                SceneRuleOverrideDefinition rule = _sceneRuleOverrides[i];
                if (rule != null && !seenRules.Add(rule.Rule))
                    Debug.LogError($"SceneSO '{name}' has duplicate Scene Rule Override: {rule.Rule}.", this);
            }

            if (_statModifiers == null) return;
            for (int i = 0; i < _statModifiers.Count; i++)
            {
                StatModifierDefinition modifier = _statModifiers[i];
                if (modifier != null && modifier.Operation == StatModifierOperation.Override)
                    Debug.LogError($"SceneSO '{name}' cannot contain Override modifiers. Put them on the active Room instead.", this);
            }
        }
#endif
    }
}
