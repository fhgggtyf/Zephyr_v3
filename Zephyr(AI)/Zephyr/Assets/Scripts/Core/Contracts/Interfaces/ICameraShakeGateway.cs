namespace Zephyr.Contracts.Interfaces
{
    /// <summary>Requests camera shake without coupling callers to a camera implementation.</summary>
    public interface ICameraShakeGateway
    {
        void ShakeForDamage(float damage);
    }
}
