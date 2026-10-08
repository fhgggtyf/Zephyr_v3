using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.Stats;

namespace Zephyr.Core.Flow
{
    [CreateAssetMenu(menuName = "Zephyr/Flow/Scene")]
    public sealed class SceneSO : ScriptableObject, IStatModifierSource
    {
        [SerializeField] private SceneId _id;
        [SerializeField] private Object _sceneAsset;
        [SerializeField, HideInInspector] private string _sceneName;
        [SerializeField, HideInInspector] private string _scenePath;
        [Header("Scene Stat Modifiers")]
        [SerializeField] private List<StatModifierDefinition> _statModifiers = new List<StatModifierDefinition>();

        public SceneId Id => _id;
        public string SceneName => _sceneName;
        public string ScenePath => _scenePath;
        public string LoadKey => string.IsNullOrWhiteSpace(_scenePath) ? _sceneName : _scenePath;
        public IReadOnlyList<StatModifierDefinition> Modifiers => _statModifiers;
        public IReadOnlyList<StatModifierDefinition> StatModifiers => _statModifiers;
        public bool IsValid => !string.IsNullOrWhiteSpace(_sceneName) &&
                               !string.IsNullOrWhiteSpace(_scenePath) &&
                               _scenePath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase);
        public bool AppliesTo(StatsCore stats) => stats != null;

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
        }
#endif
    }
}
