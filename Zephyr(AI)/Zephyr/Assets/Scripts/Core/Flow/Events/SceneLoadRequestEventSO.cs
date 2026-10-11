using UnityEngine;
using Zephyr.Core.Flow;

namespace Zephyr.Core.Events
{
    [CreateAssetMenu(menuName = "Zephyr/Events/Flow/Scene Load Request")]
    public sealed class SceneLoadRequestEventSO : GameEventSO<SceneLoadRequest> { }
}
