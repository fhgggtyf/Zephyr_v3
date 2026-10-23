using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;

namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "NewZeusBossDeathAction", menuName = "Zephyr/Enemies/New Zeus Boss/Death Action")]
    public sealed class NewZeusBossDeathActionSO : StateActionSO<NewZeusBossDeathAction> { }
}
