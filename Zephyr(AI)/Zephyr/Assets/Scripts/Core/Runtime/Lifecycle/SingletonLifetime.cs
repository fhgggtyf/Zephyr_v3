using System;
using UnityEngine;

namespace Zephyr.Core.Runtime.Lifecycle
{
    public enum SingletonLifetime
    {
        Persistent,
        GameManager,
        SceneLocal
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SingletonLifetimeAttribute : Attribute
    {
        public SingletonLifetime Lifetime { get; }
        public string OwnerScene { get; }

        public SingletonLifetimeAttribute(SingletonLifetime lifetime, string ownerScene)
        {
            Lifetime = lifetime;
            OwnerScene = ownerScene;
        }
    }

    public static class SingletonLifecycle
    {
        public static bool TryClaim<T>(T candidate, ref T current, string serviceName)
            where T : MonoBehaviour
        {
            if (candidate == null) return false;
            if (current != null && current != candidate)
            {
                Debug.LogError($"{serviceName}: duplicate lifetime owner detected. " +
                    "The existing instance owns this lifetime; the duplicate will be destroyed.", candidate);
                return false;
            }

            current = candidate;
            return true;
        }

        public static void Release<T>(T candidate, ref T current)
            where T : MonoBehaviour
        {
            if (current == candidate) current = null;
        }
    }
}
