using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;
namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewBasicEnemyPatrolAction", menuName = "Zephyr/Enemies/New Basic/Actions/Patrol")]
    public sealed class NewBasicEnemyPatrolActionSO : StateActionSO<NewBasicEnemyPatrolAction> { }
}