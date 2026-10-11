using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Zephyr.Core.StateMachine;

namespace Zephyr.Tests.EditMode
{
    public sealed class StateTransitionTests
    {
        private GameObject _owner;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("StateTransitionTests");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_owner);
        }

        [Test]
        public void TransitionWithoutConditionsIsUnconditional()
        {
            StateMachine stateMachine = _owner.AddComponent<StateMachine>();
            State target = (State)Activator.CreateInstance(typeof(State), true);
            FieldInfo machineField = typeof(State).GetField("_stateMachine", BindingFlags.Instance | BindingFlags.NonPublic);
            machineField.SetValue(target, stateMachine);

            StateTransition transition = new StateTransition(target, new StateCondition[0]);

            Assert.That(transition.TryGetTransition(out State result), Is.True);
            Assert.That(result, Is.SameAs(target));
        }
    }
}
