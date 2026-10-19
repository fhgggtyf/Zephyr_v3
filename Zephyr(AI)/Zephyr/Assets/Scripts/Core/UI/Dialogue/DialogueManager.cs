using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Zephyr.Core.Meta;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Save;
using Zephyr.Core.Flow;
using Zephyr.Core.Runtime.Lifecycle;
using Zephyr.Core.UI;

namespace Zephyr.Core.UI.Dialogue
{
    /// <summary>Runs one dialogue at a time and owns the instantiated dialogue view.</summary>
    [DefaultExecutionOrder(-50)]
    [SingletonLifetime(SingletonLifetime.GameManager, "GameManager")]
    public sealed class DialogueManager : MonoBehaviour, ZephyrInputActions.IDialogueActions,
        IDialogueGateway, IGameManagerDependencyConsumer
    {
        private static DialogueManager _instance;
        public static DialogueManager Instance => _instance;
        public static event Action GameplayInputSuspended;
        public static event Action GameplayInputRestored;
        public event Action InputSuspended;
        public event Action InputRestored;

        [SerializeField] private DialogueView _viewPrefab;
        [SerializeField] private Transform _viewParent;

        public bool IsOpen => _activeDialogue != null;
        public DialogueSO ActiveDialogue => _activeDialogue;
        public event Action<DialogueSO> DialogueStarted;
        public event Action<DialogueSO> DialogueCompleted;

        private DialogueView _view;
        private DialogueSO _activeDialogue;
        private int _lineIndex;
        private Action _completion;
        private ZephyrInputActions _inputActions;
        private bool _ownsDialogueInput;
        private ISaveDataGateway _saveData;
        private IStoryProgressionGateway _storyProgression;

        private void Awake()
        {
            if (!SingletonLifecycle.TryClaim(this, ref _instance, nameof(DialogueManager))
                || _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _inputActions = new ZephyrInputActions();
        }

        private void OnEnable()
        {
            if (_inputActions == null) return;

            _inputActions.Dialogue.AddCallbacks(this);
            if (IsOpen)
            {
                GameplayTimePause.SetModalOpen(this, true);
                CaptureDialogueInput();
            }

            SceneManager.sceneLoaded += ConfigureLoadedScene;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= ConfigureLoadedScene;
            if (_inputActions != null)
            {
                _inputActions.Dialogue.Disable();
                _inputActions.Dialogue.RemoveCallbacks(this);
            }

            ReleaseDialogueInput();
            GameplayTimePause.SetModalOpen(this, false);
        }

        private void Start()
        {
            if (_viewParent == null) _viewParent = transform;
            if (_viewPrefab == null) Debug.LogWarning("DialogueManager has no DialogueView prefab assigned.", this);
            EnsureEventSystem();
            ConfigureLoadedScenes();
        }

        public void Configure(IGameManagerServices services)
        {
            _saveData = services?.SaveData;
            _storyProgression = services?.StoryProgression;
            ConfigureLoadedScenes();
        }

        private void ConfigureLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                ConfigureLoadedScene(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
        }

        private void ConfigureLoadedScene(Scene scene, LoadSceneMode _)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                MonoBehaviour[] consumers = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
                for (int j = 0; j < consumers.Length; j++)
                {
                    if (consumers[j] is IDialogueTriggerConsumer consumer)
                        consumer.Configure(_saveData, _storyProgression, this);
                }
            }
        }

        private void OnDestroy()
        {
            Close(false);
            _inputActions?.Dispose();
            _inputActions = null;
            SingletonLifecycle.Release(this, ref _instance);
        }

        public bool TryStart(DialogueSO dialogue, Action completed = null)
        {
            if (dialogue == null || dialogue.LineCount == 0 || IsOpen) return false;
            if (_viewPrefab == null) return false;

            _view = Instantiate(_viewPrefab, _viewParent != null ? _viewParent : transform);
            _view.gameObject.SetActive(true);
            _view.AdvanceRequested += Advance;
            _activeDialogue = dialogue;
            _lineIndex = 0;
            _completion = completed;
            GameplayTimePause.SetModalOpen(this, true);
            CaptureDialogueInput();
            RefreshLine();
            DialogueStarted?.Invoke(dialogue);
            return true;
        }

        public void Advance()
        {
            if (!IsOpen) return;
            _lineIndex++;
            if (_lineIndex >= _activeDialogue.LineCount)
            {
                DialogueSO completedDialogue = _activeDialogue;
                Action callback = _completion;
                Close(false);
                callback?.Invoke();
                DialogueCompleted?.Invoke(completedDialogue);
                return;
            }

            RefreshLine();
        }

        public void Close(bool invokeCompletion = true)
        {
            if (_activeDialogue == null) return;
            Action callback = _completion;
            if (_view != null)
            {
                _view.AdvanceRequested -= Advance;
                Destroy(_view.gameObject);
            }

            _view = null;
            _activeDialogue = null;
            _lineIndex = 0;
            _completion = null;
            ReleaseDialogueInput();
            GameplayTimePause.SetModalOpen(this, false);
            if (invokeCompletion) callback?.Invoke();
        }

        public void OnNextLine(InputAction.CallbackContext context)
        {
            if (context.performed && IsOpen) Advance();
        }

        private void RefreshLine()
        {
            if (_view == null || _activeDialogue == null) return;
            DialogueLine line = _activeDialogue.GetLine(_lineIndex);
            DialogueParticipant participant = _activeDialogue.GetParticipant(line.Speaker);
            _view.SetLine(line, participant, participant.ResolveName(), line.ResolveText(),
                _lineIndex, _activeDialogue.LineCount);
            _view.SetContinueVisible(true);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject eventSystem = new GameObject("DialogueEventSystem", typeof(EventSystem));
            Type inputModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null && typeof(BaseInputModule).IsAssignableFrom(inputModuleType))
                eventSystem.AddComponent(inputModuleType);
            else
                eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void CaptureDialogueInput()
        {
            if (_ownsDialogueInput || _inputActions == null) return;

            _ownsDialogueInput = true;
            GameplayInputSuspended?.Invoke();
            InputSuspended?.Invoke();
            _inputActions.Dialogue.Enable();
        }

        private void ReleaseDialogueInput()
        {
            if (!_ownsDialogueInput) return;

            _inputActions?.Dialogue.Disable();
            _ownsDialogueInput = false;
            GameplayInputRestored?.Invoke();
            InputRestored?.Invoke();
        }

    }
}
