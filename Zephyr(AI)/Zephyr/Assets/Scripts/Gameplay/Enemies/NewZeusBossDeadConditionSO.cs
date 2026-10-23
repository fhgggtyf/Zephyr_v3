using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;

namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewZeusBossDeadCondition", menuName = "Zephyr/Enemies/New Zeus Boss/Dead Condition")]
    public sealed class NewZeusBossDeadConditionSO : StateConditionSO<NewZeusBossDeadCondition> { }
}
