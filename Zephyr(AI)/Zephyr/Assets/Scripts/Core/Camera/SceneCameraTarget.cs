using UnityEngine;

namespace Zephyr.Core.Camera
{
    public sealed class SceneCameraTarget : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        private void OnEnable() => Bind();

        private void Start() => Bind();

        private void Bind()
        {
            if (_target == null) return;
            CameraController.FindForGameplay(gameObject.scene)?.SetFollow(_target);
        }
    }
}
