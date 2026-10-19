using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Core.Flow;
using Zephyr.Core.Runtime.Stats;
using Zephyr.Core.Runtime.Lifecycle;

namespace Zephyr.Core.Stats
{
    [DefaultExecutionOrder(-250)]
    [SingletonLifetime(SingletonLifetime.GameManager, "GameManager")]
    public sealed class SceneStatsModifierController : MonoBehaviour, IStatModifierSource, ISceneRuleSource
    {
        private static SceneStatsModifierController _instance;
        public static SceneStatsModifierController Instance => _instance;

        [Header("SceneSO catalog")]
        [Tooltip("SceneSO assets used to resolve the active scene by path.")]
        [SerializeField] private List<SceneSO> _sceneCatalog = new List<SceneSO>();

        private readonly List<IStatModifierSource> _externalSources = new List<IStatModifierSource>();
        private readonly List<StatModifierDefinition> _activeSceneModifiers = new List<StatModifierDefinition>();
        private readonly List<SceneRuleOverrideDefinition> _activeSceneRuleOverrides = new List<SceneRuleOverrideDefinition>();
        private SceneSO _activeScene;

        public IReadOnlyList<StatModifierDefinition> Modifiers => _activeSceneModifiers;
        public IReadOnlyList<SceneRuleOverrideDefinition> RuleOverrides => _activeSceneRuleOverrides;

        private void Awake()
        {
            if (!SingletonLifecycle.TryClaim(this, ref _instance, nameof(SceneStatsModifierController))
                || _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            StatModifierRegistry.Register(this);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            RefreshActiveScene(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            StatModifierRegistry.Unregister(this);
            for (int i = 0; i < _externalSources.Count; i++)
                StatModifierRegistry.Unregister(_externalSources[i]);
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            SingletonLifecycle.Release(this, ref _instance);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Unity invokes sceneLoaded before SceneLoader switches the active
            // scene. Resolve the scene that actually finished loading instead
            // of reading the previous active scene.
            RefreshActiveScene(scene);
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (_activeScene != null && string.Equals(_activeScene.ScenePath, scene.path, System.StringComparison.OrdinalIgnoreCase))
                RefreshActiveScene(SceneManager.GetActiveScene());
        }

        private void RefreshActiveScene(Scene scene)
        {
            _activeScene = ResolveScene(scene);
            _activeSceneModifiers.Clear();
            _activeSceneRuleOverrides.Clear();
            if (_activeScene == null) return;

            if (_activeScene.StatModifiers != null)
            {
                for (int i = 0; i < _activeScene.StatModifiers.Count; i++)
                {
                    StatModifierDefinition definition = _activeScene.StatModifiers[i];
                    if (definition != null) _activeSceneModifiers.Add(definition);
                }
            }

            if (_activeScene.RuleOverrides != null)
            {
                for (int i = 0; i < _activeScene.RuleOverrides.Count; i++)
                {
                    SceneRuleOverrideDefinition definition = _activeScene.RuleOverrides[i];
                    if (definition != null) _activeSceneRuleOverrides.Add(definition);
                }
            }
        }

        private SceneSO ResolveScene(Scene scene)
        {
            if (!scene.IsValid()) return null;

            for (int i = 0; i < _sceneCatalog.Count; i++)
            {
                SceneSO candidate = _sceneCatalog[i];
                if (candidate != null && string.Equals(candidate.ScenePath, scene.path, System.StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }

        public bool RegisterSource(IStatModifierSource source)
        {
            if (source == null || System.Object.ReferenceEquals(source, this) || _externalSources.Contains(source)) return false;
            _externalSources.Add(source);
            StatModifierRegistry.Register(source);
            return true;
        }

        public bool UnregisterSource(IStatModifierSource source)
        {
            if (source == null || !_externalSources.Remove(source)) return false;
            StatModifierRegistry.Unregister(source);
            return true;
        }

        public bool TryGetActiveScene(out SceneSO scene)
        {
            scene = _activeScene;
            return scene != null;
        }

        public bool AppliesTo(IStatModifierTarget target) => target != null;
    }
}
