using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Core.Flow;

namespace Zephyr.Core.Stats
{
    [DefaultExecutionOrder(-250)]
    public sealed class SceneStatsModifierController : MonoBehaviour, IStatModifierSource
    {
        public static SceneStatsModifierController Instance { get; private set; }

        [Header("SceneSO catalog")]
        [Tooltip("SceneSO assets used to resolve the active scene by path.")]
        [SerializeField] private List<SceneSO> _sceneCatalog = new List<SceneSO>();

        private readonly List<IStatModifierSource> _externalSources = new List<IStatModifierSource>();
        private readonly List<StatModifierDefinition> _activeSceneModifiers = new List<StatModifierDefinition>();
        private SceneSO _activeScene;

        public IReadOnlyList<StatModifierDefinition> Modifiers => _activeSceneModifiers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            RefreshActiveScene(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            Instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshActiveScene(SceneManager.GetActiveScene());
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
            if (_activeScene == null || _activeScene.StatModifiers == null) return;

            for (int i = 0; i < _activeScene.StatModifiers.Count; i++)
            {
                StatModifierDefinition definition = _activeScene.StatModifiers[i];
                if (definition != null) _activeSceneModifiers.Add(definition);
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

            SceneContext[] contexts = FindObjectsByType<SceneContext>(FindObjectsInactive.Include);
            for (int i = 0; i < contexts.Length; i++)
            {
                SceneContext context = contexts[i];
                if (context != null && context.gameObject.scene == scene)
                    return context.Scene;
            }

            return null;
        }

        public bool RegisterSource(IStatModifierSource source)
        {
            if (source == null || System.Object.ReferenceEquals(source, this) || _externalSources.Contains(source)) return false;
            _externalSources.Add(source);
            return true;
        }

        public bool UnregisterSource(IStatModifierSource source)
        {
            return source != null && _externalSources.Remove(source);
        }

        public bool TryGetActiveScene(out SceneSO scene)
        {
            scene = _activeScene;
            return scene != null;
        }

        public bool AppliesTo(StatsCore stats) => stats != null;

        internal static void ApplyExternalModifiers(StatsCore stats, ref float value, StatModifierTargetKind targetKind, StatType statType, MetaStatType metaStatType, string category)
        {
            SceneStatsModifierController controller = Instance;
            if (controller == null || stats == null) return;

            controller.ApplyDefinitions(controller._activeSceneModifiers, stats, ref value, targetKind, statType, metaStatType, category);
            for (int i = controller._externalSources.Count - 1; i >= 0; i--)
            {
                IStatModifierSource source = controller._externalSources[i];
                if (source == null || !source.AppliesTo(stats))
                {
                    if (source == null) controller._externalSources.RemoveAt(i);
                    continue;
                }
                controller.ApplyDefinitions(source.Modifiers, stats, ref value, targetKind, statType, metaStatType, category);
            }
        }

        private void ApplyDefinitions(IReadOnlyList<StatModifierDefinition> definitions, StatsCore stats, ref float value, StatModifierTargetKind targetKind, StatType statType, MetaStatType metaStatType, string category)
        {
            if (definitions == null) return;

            for (int i = 0; i < definitions.Count; i++)
            {
                StatModifierDefinition definition = definitions[i];
                if (definition == null || definition.TargetKind != targetKind || !definition.AppliesTo(stats)) continue;
                if (targetKind == StatModifierTargetKind.CoreStat && definition.StatType != statType) continue;
                if (targetKind == StatModifierTargetKind.MetaStat && definition.MetaStatType != metaStatType) continue;
                if (targetKind == StatModifierTargetKind.CategoryMultiplier && !string.Equals(definition.Category, category, System.StringComparison.Ordinal)) continue;

                switch (definition.Operation)
                {
                    case StatModifierOperation.Add: value += definition.Value; break;
                    case StatModifierOperation.Multiply: value *= definition.Value; break;
                    case StatModifierOperation.Override: value = definition.Value; break;
                }
            }
        }
    }
}
