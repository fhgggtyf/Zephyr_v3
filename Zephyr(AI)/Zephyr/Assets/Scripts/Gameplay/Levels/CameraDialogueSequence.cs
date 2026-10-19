using System.Collections;
using UnityEngine;
using Zephyr.Core.Camera;
using Zephyr.Core.Meta;
using Zephyr.Core.Save;
using Zephyr.Core.UI;
using Zephyr.Core.UI.Dialogue;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Gameplay.Levels
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CameraDialogueSequence : MonoBehaviour, IDialogueTriggerConsumer
    {
        [SerializeField] private DialogueSO _dialogue;
        [SerializeField] private Transform _focusPoint;
        [SerializeField, Min(0f)] private float _travelDuration = 1.2f;
        [SerializeField, Min(0f)] private float _returnDuration = 0.8f;
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private LayerMask _playerLayers = -1;

        private bool _started;
        private IDialogueGateway _dialogueGateway;
        private CameraController _camera;
        private Coroutine _sequence;

        public void Configure(ISaveDataGateway saveData, IStoryProgressionGateway storyProgression,
            IDialogueGateway dialogueGateway)
        {
            _dialogueGateway = dialogueGateway;
        }

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            _playerLayers = -1;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other)) TryStart();
        }

        public bool TryStart()
        {
            if (_started || _dialogue == null || _focusPoint == null || _dialogueGateway == null) return false;

            _camera = CameraController.FindForGameplay(gameObject.scene);
            if (_camera == null || !_camera.BeginGuidance(_focusPoint.position, _travelDuration)) return false;

            _started = true;
            GameplayTimePause.SetModalOpen(this, true);
            _sequence = StartCoroutine(RunSequence());
            return true;
        }

        private IEnumerator RunSequence()
        {
            while (_camera != null && !_camera.IsGuidanceAtTarget)
                yield return null;

            if (_camera == null || !_dialogueGateway.TryStart(_dialogue, HandleDialogueCompleted))
                FinishSequence();
        }

        private void HandleDialogueCompleted()
        {
            if (_camera == null)
            {
                FinishSequence();
                return;
            }

            _camera.EndGuidance(_returnDuration, FinishSequence);
        }

        private void FinishSequence()
        {
            if (_sequence != null)
            {
                StopCoroutine(_sequence);
                _sequence = null;
            }

            GameplayTimePause.SetModalOpen(this, false);
            if (!_triggerOnce) _started = false;
        }

        private void OnDisable()
        {
            GameplayTimePause.SetModalOpen(this, false);
            if (_sequence != null) StopCoroutine(_sequence);
            _sequence = null;
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null || (_playerLayers.value & (1 << other.gameObject.layer)) == 0) return false;
            if (string.IsNullOrWhiteSpace(_playerTag)) return true;
            try { return other.transform.root.CompareTag(_playerTag); }
            catch (UnityException) { return false; }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.15f, 0.8f);
            Collider2D collider = GetComponent<Collider2D>();
            if (collider is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }

            if (_focusPoint != null)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawWireSphere(_focusPoint.position, 0.25f);
                Gizmos.DrawLine(transform.position, _focusPoint.position);
            }
        }
    }
}
