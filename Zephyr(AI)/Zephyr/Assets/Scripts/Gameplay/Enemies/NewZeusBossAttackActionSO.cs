using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;

namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewZeusBossAttackAction", menuName = "Zephyr/Enemies/New Zeus Boss/Attack Action")]
    public sealed class NewZeusBossAttackActionSO : StateActionSO<NewZeusBossAttackAction> { }
}
