using UnityEngine;
using Zephyr.Core.Flow;

namespace Zephyr.Core.Events
{
    [CreateAssetMenu(menuName = "Zephyr/Events/Flow/Game State")]
    public sealed class GameStateEventSO : GameEventSO<GameState> { }
}
