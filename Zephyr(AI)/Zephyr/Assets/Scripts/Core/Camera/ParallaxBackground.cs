using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Zephyr.Core.Camera
{
    [DisallowMultipleComponent]
    public sealed class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private Vector2 _screenOffset;

        private ParallaxLayer[] _layers;
        private CameraController _cameraController;
        private UnityEngine.Camera _outputCamera;
        private Vector3 _lastCameraPosition;
        private bool _hasCameraPosition;

        private void Awake() => CacheLayers();

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            ResolveCameraController();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            _cameraController = null;
            _outputCamera = null;
            _hasCameraPosition = false;
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            CacheLayers();
        }

        private void HandleBeginCameraRendering(ScriptableRenderContext context, UnityEngine.Camera renderingCamera)
        {
            if (_outputCamera == null)
                ResolveCameraController();
            if (renderingCamera == null || renderingCamera != _outputCamera) return;

            Vector3 cameraPosition = renderingCamera.transform.position;
            if (!_hasCameraPosition)
            {
                AnchorToCamera(cameraPosition);
                _lastCameraPosition = cameraPosition;
                _hasCameraPosition = true;
                return;
            }

            Vector3 cameraDelta = cameraPosition - _lastCameraPosition;
            transform.position += cameraDelta;
            foreach (ParallaxLayer layer in _layers)
            {
                if (layer != null)
                    layer.ApplyCameraDelta(cameraDelta, renderingCamera);
            }

            _lastCameraPosition = cameraPosition;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => ResolveCameraController();

        private void HandleSceneUnloaded(Scene scene)
        {
            if (_cameraController != null && _cameraController.gameObject.scene == scene)
            {
                _cameraController = null;
                _outputCamera = null;
                _hasCameraPosition = false;
            }

            ResolveCameraController();
        }

        private void HandleActiveSceneChanged(Scene previous, Scene current) => ResolveCameraController();

        private void ResolveCameraController()
        {
            CameraController controller = CameraController.FindForGameplay(gameObject.scene);
            UnityEngine.Camera outputCamera = controller != null ? controller.OutputCamera : null;
            if (controller == _cameraController && outputCamera == _outputCamera) return;

            _cameraController = controller;
            _outputCamera = outputCamera;
            _hasCameraPosition = false;
        }

        private void AnchorToCamera(Vector3 cameraPosition)
        {
            Vector3 position = transform.position;
            position.x = cameraPosition.x + _screenOffset.x;
            position.y = cameraPosition.y + _screenOffset.y;
            transform.position = position;

            CacheLayers();
            foreach (ParallaxLayer layer in _layers)
            {
                if (layer != null)
                    layer.ResetToAuthoredLayout();
            }
        }

        private void CacheLayers()
        {
            _layers = GetComponentsInChildren<ParallaxLayer>(true);
        }
    }
}
