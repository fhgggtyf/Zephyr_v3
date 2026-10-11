using System;
using UnityEngine;
using Zephyr.Core.Meta;
using Zephyr.Core.Save;

namespace Zephyr.Core.Flow
{
    [DefaultExecutionOrder(-100)]
    public sealed class PersistentBootstrap : MonoBehaviour
    {
        public static bool IsReady { get; private set; }
        public static event Action Ready;
        private IMetaGateway _metaGateway;

        private void Awake()
        {
            _metaGateway = new MetaGateway(SaveSystem.Instance);
        }

        private void Start()
        {
            StoryProgressionService.Instance?.Configure(_metaGateway);
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
