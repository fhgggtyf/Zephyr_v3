using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Core.Events;
using Zephyr.Core.Runtime.Lifecycle;
using Zephyr.Contracts.Flow;

namespace Zephyr.Core.Flow
{
    [DefaultExecutionOrder(-800)]
    [SingletonLifetime(SingletonLifetime.Persistent, "Persistent")]
    public sealed class SceneLoader : MonoBehaviour, ISceneLoadingGateway
    {
        private static SceneLoader _instance;
        public static SceneLoader Instance => _instance;

        [Header("Event Channels")]
        [SerializeField] private GameEventSO<SceneLoadRequest> _onRequestSceneLoad;
        [SerializeField] private GameEventSO<SceneSO> _onRequestSceneUnload;
        [SerializeField] private GameEventSO<SceneInfo> _onSceneLoaded;

        [Header("Loading Screen")]
        [SerializeField] private GameObject _loadingScreen;

        private readonly Queue<SceneOperation> _operations = new Queue<SceneOperation>();
        private readonly Dictionary<SceneId, bool> _expectedLoadedStates = new Dictionary<SceneId, bool>();
        private bool _isProcessing;
        private bool _loadFailed;

        public event Action<SceneInfo> SceneLoaded;
        public bool IsLoading => _isProcessing;
        public event Action LoadingStarted;
        public event Action LoadingCompleted;
        public event Action LoadingFailed;

        private void Awake()
        {
            if (!SingletonLifecycle.TryClaim(this, ref _instance, nameof(SceneLoader))
                || _instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            _onRequestSceneLoad?.Subscribe(HandleSceneLoadRequest);
            _onRequestSceneUnload?.Subscribe(HandleSceneUnloadRequest);
        }

        private void OnDisable()
        {
            _onRequestSceneLoad?.Unsubscribe(HandleSceneLoadRequest);
            _onRequestSceneUnload?.Unsubscribe(HandleSceneUnloadRequest);

            StopAllCoroutines();
            _operations.Clear();
            _expectedLoadedStates.Clear();
            _isProcessing = false;
            HideLoadingScreen();
        }

        private void OnDestroy()
        {
            SingletonLifecycle.Release(this, ref _instance);
        }

        private void HandleSceneLoadRequest(SceneLoadRequest request)
        {
            if (!ValidateScene(request.TargetScene, "target")) return;

            bool targetAlreadyExpected = IsExpectedLoaded(request.TargetScene);
            bool replacementNeedsUnload = request.SceneToReplace != null &&
                                          request.SceneToReplace != request.TargetScene &&
                                          IsExpectedLoaded(request.SceneToReplace);

            if (targetAlreadyExpected && !replacementNeedsUnload) return;

            _operations.Enqueue(SceneOperation.CreateTransition(request));
            _expectedLoadedStates[request.TargetScene.Id] = true;
            if (request.SceneToReplace != null && request.SceneToReplace != request.TargetScene)
                _expectedLoadedStates[request.SceneToReplace.Id] = false;
            StartProcessor();
        }

        /// <summary>
        /// Enqueues a scene load through this SceneLoader instance.
        /// This is used by direct-open bootstrap code after Persistent is loaded,
        /// while authored gameplay flow continues to use the event channel.
        /// </summary>
        public bool RequestSceneLoad(SceneSO targetScene, SceneSO sceneToReplace = null,
            LoadSceneMode mode = LoadSceneMode.Additive)
        {
            if (!ValidateScene(targetScene, "target")) return false;

            HandleSceneLoadRequest(new SceneLoadRequest(targetScene, sceneToReplace, mode));
            return true;
        }


        private void HandleSceneUnloadRequest(SceneSO scene)
        {
            if (!ValidateScene(scene, "unload") || !IsExpectedLoaded(scene)) return;

            _operations.Enqueue(SceneOperation.CreateUnload(scene));
            _expectedLoadedStates[scene.Id] = false;
            StartProcessor();
        }

        private void StartProcessor()
        {
            if (_isProcessing) return;
            _isProcessing = true;
            StartCoroutine(ProcessOperations());
        }

        private IEnumerator ProcessOperations()
        {
            _loadFailed = false;
            LoadingStarted?.Invoke();
            ShowLoadingScreen();
            yield return null;

            while (_operations.Count > 0)
            {
                SceneOperation operation = _operations.Dequeue();

                if (operation.SceneToReplace != null && operation.SceneToReplace != operation.TargetScene)
                    yield return UnloadSceneAsync(operation.SceneToReplace);

                if (operation.TargetScene != null)
                    yield return LoadSceneAsync(operation.TargetScene, operation.Mode);

                if (_loadFailed) break;
            }

            _expectedLoadedStates.Clear();
            HideLoadingScreen();
            _isProcessing = false;
            if (_loadFailed) LoadingFailed?.Invoke();
            else LoadingCompleted?.Invoke();
        }

        private IEnumerator LoadSceneAsync(SceneSO scene, LoadSceneMode mode)
        {
            if (IsSceneLoaded(scene)) yield break;

            AsyncOperation operation = SceneManager.LoadSceneAsync(scene.LoadKey, mode);
            if (operation == null)
            {
                Debug.LogError($"SceneLoader: Unity could not start loading '{scene.SceneName}'.", scene);
                _loadFailed = true;
                yield break;
            }

            yield return operation;

            Scene loadedScene = SceneManager.GetSceneByName(scene.SceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                if (!SceneManager.SetActiveScene(loadedScene))
                    Debug.LogWarning($"SceneLoader: failed to set '{scene.SceneName}' as the active scene.", scene);
            }

            if (scene.Id == SceneId.GameManager)
                yield return new WaitUntil(() => GameManagerBootstrap.IsReady);

            SceneInfo info = new SceneInfo(scene);
            _onSceneLoaded?.Raise(info);
            SceneLoaded?.Invoke(info);
        }

        private static IEnumerator UnloadSceneAsync(SceneSO scene)
        {
            if (!IsSceneLoaded(scene)) yield break;

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene.SceneName);
            if (operation != null) yield return operation;
        }

        private bool IsExpectedLoaded(SceneSO scene)
        {
            return _expectedLoadedStates.TryGetValue(scene.Id, out bool expected)
                ? expected
                : IsSceneLoaded(scene);
        }

        private static bool IsSceneLoaded(SceneSO scene)
        {
            if (scene == null) return false;
            Scene loadedScene = SceneManager.GetSceneByName(scene.SceneName);
            return loadedScene.IsValid() && loadedScene.isLoaded;
        }

        private static bool ValidateScene(SceneSO scene, string role)
        {
            if (scene != null && scene.IsValid) return true;
            Debug.LogError($"SceneLoader: {role} SceneSO is missing or invalid.", scene);
            return false;
        }

        private void ShowLoadingScreen()
        {
            if (_loadingScreen != null) _loadingScreen.SetActive(true);
        }

        private void HideLoadingScreen()
        {
            if (_loadingScreen != null) _loadingScreen.SetActive(false);
        }

        private readonly struct SceneOperation
        {
            public readonly SceneSO TargetScene;
            public readonly SceneSO SceneToReplace;
            public readonly LoadSceneMode Mode;

            private SceneOperation(SceneSO targetScene, SceneSO sceneToReplace, LoadSceneMode mode)
            {
                TargetScene = targetScene;
                SceneToReplace = sceneToReplace;
                Mode = mode;
            }

            public static SceneOperation CreateTransition(SceneLoadRequest request)
                => new SceneOperation(request.TargetScene, request.SceneToReplace, request.Mode);

            public static SceneOperation CreateUnload(SceneSO scene)
                => new SceneOperation(null, scene, LoadSceneMode.Additive);
        }
    }

    public readonly struct SceneLoadRequest
    {
        public readonly SceneSO TargetScene;
        public readonly SceneSO SceneToReplace;
        public readonly LoadSceneMode Mode;

        public SceneLoadRequest(SceneSO targetScene, SceneSO sceneToReplace = null,
            LoadSceneMode mode = LoadSceneMode.Additive)
        {
            TargetScene = targetScene;
            SceneToReplace = sceneToReplace;
            Mode = mode;
        }
    }

    public readonly struct SceneInfo
    {
        public readonly SceneSO Scene;
        public SceneId SceneId => Scene != null ? Scene.Id : default;
        public string SceneName => Scene != null ? Scene.SceneName : string.Empty;

        public SceneInfo(SceneSO scene) => Scene = scene;
    }
}
