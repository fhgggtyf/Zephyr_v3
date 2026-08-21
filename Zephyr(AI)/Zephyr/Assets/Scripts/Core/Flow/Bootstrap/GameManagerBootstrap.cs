using UnityEngine;

namespace Zephyr.Core.Flow
{
    /// <summary>
    /// Scene-local composition root for gameplay-only managers.
    /// Concrete camera/UI/projectile/combat systems can be attached here as they land.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class GameManagerBootstrap : MonoBehaviour
    {
        public static bool IsReady { get; private set; }

        private void Start()
        {
            IsReady = true;
        }

        private void OnDestroy() => IsReady = false;
    }
}
