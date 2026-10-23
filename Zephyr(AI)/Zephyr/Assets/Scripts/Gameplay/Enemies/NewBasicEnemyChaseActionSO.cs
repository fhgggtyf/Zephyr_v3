using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;
namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewBasicEnemyChaseAction", menuName = "Zephyr/Enemies/New Basic/Actions/Chase")]
    public sealed class NewBasicEnemyChaseActionSO : StateActionSO<NewBasicEnemyChaseAction> { }
}