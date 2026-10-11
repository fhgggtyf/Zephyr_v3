using UnityEngine;
using Zephyr.Core.Flow;

namespace Zephyr.Core.Events
{
    [CreateAssetMenu(menuName = "Zephyr/Events/Flow/Scene Reference")]
    public sealed class SceneIdEventSO : GameEventSO<SceneSO> { }
}
