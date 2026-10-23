using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;
namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewBasicEnemyAlertAction", menuName = "Zephyr/Enemies/New Basic/Actions/Alert")]
    public sealed class NewBasicEnemyAlertActionSO : StateActionSO<NewBasicEnemyAlertAction> { }
}