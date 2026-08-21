using UnityEngine;

namespace Zephyr.Core.Flow.Scenes
{
    public sealed class MetaHubController : MonoBehaviour
    {
        public void StartRun() => GameFlow.Instance?.RequestTransition(GameState.LoadingRun);
    }
}
