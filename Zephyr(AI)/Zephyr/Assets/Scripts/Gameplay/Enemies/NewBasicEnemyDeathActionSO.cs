using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;
namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewBasicEnemyDeathAction", menuName = "Zephyr/Enemies/New Basic/Actions/Death")]
    public sealed class NewBasicEnemyDeathActionSO : StateActionSO<NewBasicEnemyDeathAction> { }
}