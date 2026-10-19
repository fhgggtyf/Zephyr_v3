using UnityEngine;

namespace Zephyr.Contracts.Interfaces
{
    public interface IGustJumpReceiver
    {
        bool CanRequestGustJump { get; }
        void RequestGustJump(Transform destination);
    }
}
