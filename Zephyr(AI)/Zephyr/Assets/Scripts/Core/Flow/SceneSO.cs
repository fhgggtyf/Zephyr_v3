using UnityEngine;

namespace Zephyr.Core.Flow
{
    [CreateAssetMenu(menuName = "Zephyr/Flow/Scene")]
    public sealed class SceneSO : ScriptableObject
    {
        [SerializeField] private SceneId _id;
        [SerializeField] private Object _sceneAsset;
        [SerializeField, HideInInspector] private string _sceneName;
        [SerializeField, HideInInspector] private string _scenePath;

        public SceneId Id => _id;
        public string SceneName => _sceneName;
        public string ScenePath => _scenePath;
        public string LoadKey => string.IsNullOrWhiteSpace(_scenePath) ? _sceneName : _scenePath;
        public bool IsValid => !string.IsNullOrWhiteSpace(_sceneName) &&
                               !string.IsNullOrWhiteSpace(_scenePath) &&
                               _scenePath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase);

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
