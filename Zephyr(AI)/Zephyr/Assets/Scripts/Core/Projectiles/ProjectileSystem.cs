using UnityEngine;
using Zephyr.Core.DamageSystem;

namespace Zephyr.Core.Projectiles
{
    /// <summary>Gameplay-scene projectile owner; actual projectile prefabs are upper-layer assets.</summary>
    public sealed class ProjectileSystem : MonoBehaviour
    {
        public int ActiveProjectileCount { get; private set; }
        public event System.Action<ProjectileLaunchData> ProjectileLaunched;
        public void RegisterProjectile() => ActiveProjectileCount++;
        public void UnregisterProjectile() => ActiveProjectileCount = Mathf.Max(0, ActiveProjectileCount - 1);
        public void Launch(in ProjectileLaunchData data)
        {
            ActiveProjectileCount++;
            ProjectileLaunched?.Invoke(data);
        }
        private void OnDisable() => ActiveProjectileCount = 0;
    }

    public readonly struct ProjectileLaunchData
    {
        public readonly Vector2 Origin;
        public readonly Vector2 Velocity;
        public readonly bool AffectedByGravity;
        public readonly float GravityScale;
        public readonly float Lifetime;
        public readonly int PierceCount;
        public readonly DamageInfo DamageOnHit;

        public ProjectileLaunchData(Vector2 origin, Vector2 velocity, bool affectedByGravity,
            float gravityScale, float lifetime, int pierceCount, DamageInfo damageOnHit)
        {
            Origin = origin;
            Velocity = velocity;
            AffectedByGravity = affectedByGravity;
            GravityScale = gravityScale;
            Lifetime = lifetime;
            PierceCount = Mathf.Max(0, pierceCount);
            DamageOnHit = damageOnHit;
        }
    }
}
