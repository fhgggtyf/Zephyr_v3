using System;
using UnityEngine;
using Zephyr.Core.Save;

namespace Zephyr.Core.Flow
{
    [DefaultExecutionOrder(-100)]
    public sealed class PersistentBootstrap : MonoBehaviour
    {
        public static bool IsReady { get; private set; }
        public static event Action Ready;

        private void Start()
        {
            IsReady = true;
            Ready?.Invoke();
        }

        private void OnDestroy()
        {
            IsReady = false;
            Ready = null;
        }
    }
}
