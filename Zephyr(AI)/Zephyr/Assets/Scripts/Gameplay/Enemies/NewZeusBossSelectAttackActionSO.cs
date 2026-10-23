using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;

namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewZeusBossSelectAttackAction", menuName = "Zephyr/Enemies/New Zeus Boss/Select Attack Action")]
    public sealed class NewZeusBossSelectAttackActionSO : StateActionSO<NewZeusBossSelectAttackAction> { }
}
