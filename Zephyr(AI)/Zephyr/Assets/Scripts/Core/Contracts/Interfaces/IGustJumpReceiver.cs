using UnityEngine;

namespace Zephyr.Core.Interfaces
{
    public interface IGustJumpReceiver
    {
        bool CanRequestGustJump { get; }
        void RequestGustJump(Transform destination);
    }
}
