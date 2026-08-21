using UnityEngine;

namespace Zephyr.Core.Flow
{
    /// <summary>
    /// Scene-level reference used by scene-owned systems without relying on names.
    /// </summary>
    public sealed class SceneContext : MonoBehaviour
    {
        [SerializeField] private SceneSO _scene;

        public SceneSO Scene => _scene;
        public SceneId SceneId => _scene != null ? _scene.Id : default;
    }
}
