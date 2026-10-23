using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;
namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewBasicEnemyHurtAction", menuName = "Zephyr/Enemies/New Basic/Actions/Hurt")]
    public sealed class NewBasicEnemyHurtActionSO : StateActionSO<NewBasicEnemyHurtAction> { }
}