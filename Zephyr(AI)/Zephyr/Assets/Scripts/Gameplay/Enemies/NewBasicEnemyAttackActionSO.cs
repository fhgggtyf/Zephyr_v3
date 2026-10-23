using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;
namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewBasicEnemyAttackAction", menuName = "Zephyr/Enemies/New Basic/Actions/Attack")]
    public sealed class NewBasicEnemyAttackActionSO : StateActionSO<NewBasicEnemyAttackAction> { }
}