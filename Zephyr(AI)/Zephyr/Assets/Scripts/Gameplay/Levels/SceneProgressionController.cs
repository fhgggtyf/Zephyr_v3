using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Core.Flow;

namespace Zephyr.Gameplay.Levels
{
    /// <summary>
    /// Owns the first authored scene progression loop. It only emits requests to
    /// GameFlow; SceneLoader remains the sole authority that loads/unloads scenes.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public sealed class SceneProgressionController : MonoBehaviour
    {
        public static SceneProgressionController Instance { get; private set; }

        [Header("Tutorial")]
        [SerializeField] private SceneSO _tutorialPhase1;
        [SerializeField] private SceneSO _tutorialPhase2;

        [Header("Hub and run placeholders")]
        [SerializeField] private SceneSO _metaHub;
        [SerializeField] private SceneSO _lv1;
        [SerializeField] private SceneSO _lv2;
        [SerializeField] private SceneSO _lv3;

        public event Action<SceneSO, SceneSO> ProgressionRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool AdvanceFromCurrentScene()
        {
            SceneSO current = FindLoadedProgressionScene();
            if (current == null)
            {
                Debug.LogWarning("SceneProgressionController: no authored progression scene is loaded.", this);
                return false;
            }

            SceneSO next = GetNext(current);
            if (next == null)
            {
                Debug.LogError($"SceneProgressionController: no next scene configured for '{current.SceneName}'.", this);
                return false;
            }

            GameState nextState = GetLogicalState(next);
            ProgressionRequested?.Invoke(current, next);
            GameFlow.Instance?.RequestProgressionSceneTransition(next, current, nextState);
            return true;
        }

        public SceneSO GetNext(SceneSO current)
        {
            if (current == null) return null;
            if (Matches(current, _tutorialPhase1)) return _tutorialPhase2;
            if (Matches(current, _tutorialPhase2)) return _metaHub;
            if (Matches(current, _metaHub)) return _lv1;
            if (Matches(current, _lv1)) return _lv2;
            if (Matches(current, _lv2)) return _lv3;
            if (Matches(current, _lv3)) return _metaHub;
            return null;
        }

        private SceneSO FindLoadedProgressionScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                if (Matches(scene, _tutorialPhase1)) return _tutorialPhase1;
                if (Matches(scene, _tutorialPhase2)) return _tutorialPhase2;
                if (Matches(scene, _metaHub)) return _metaHub;
                if (Matches(scene, _lv1)) return _lv1;
                if (Matches(scene, _lv2)) return _lv2;
                if (Matches(scene, _lv3)) return _lv3;
            }

            return null;
        }

        private static bool Matches(SceneSO scene, SceneSO candidate)
            => scene != null && candidate != null && scene.ScenePath == candidate.ScenePath;

        private static bool Matches(Scene scene, SceneSO candidate)
            => candidate != null && scene.path == candidate.ScenePath;

        private static GameState GetLogicalState(SceneSO scene)
        {
            if (scene == null) return GameState.Boot;
            string name = scene.SceneName;
            if (name.StartsWith("Tutorial_", StringComparison.OrdinalIgnoreCase)) return GameState.Tutorial;
            if (string.Equals(name, "MetaHub", StringComparison.OrdinalIgnoreCase)) return GameState.MetaHub;
            return GameState.InRun;
        }
    }
}
