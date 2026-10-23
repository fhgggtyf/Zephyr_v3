using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;
namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewBasicEnemyIdleAction", menuName = "Zephyr/Enemies/New Basic/Actions/Idle")]
    public sealed class NewBasicEnemyIdleActionSO : StateActionSO<NewBasicEnemyIdleAction> { }
}