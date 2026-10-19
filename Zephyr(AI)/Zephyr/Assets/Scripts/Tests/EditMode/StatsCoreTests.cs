using NUnit.Framework;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.Stats;

namespace Zephyr.Tests.EditMode
{
    public sealed class StatsCoreTests
    {
        private GameObject _owner;
        private StatsCore _stats;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("StatsCoreTests");
            _stats = _owner.AddComponent<StatsCore>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
        }

        [Test]
        public void RunModifiersAreIncludedAndClearedAtRunStart()
        {
            _stats.SetBase(StatType.Attack, 10f);
            _stats.AddModifier(StatType.Attack, 3f);
            _stats.AddTemporaryModifier(StatType.Attack, 2f);

            Assert.That(_stats.GetStatValue(StatType.Attack), Is.EqualTo(15f).Within(0.001f));

            _stats.ResetForRunStart();

            Assert.That(_stats.GetStatValue(StatType.Attack), Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void FourLayerFormulaAddsNumbersBeforeSummingPercentages()
        {
            _stats.SetBase(StatType.MaxHp, 100f);
            _stats.AddMetaBase(StatType.MaxHp, 50f);
            _stats.AddMetaBasePercent(StatType.MaxHp, 0.3f);
            _stats.SetRunPotential(StatType.MaxHp, 10f);
            _stats.ApplyPotentialScaledGain(StatType.MaxHp, 30f);
            _stats.AddTemporaryModifierPercent(StatType.MaxHp, 0.2f);

            Assert.That(_stats.GetStatValue(StatType.MaxHp), Is.EqualTo(270f).Within(0.001f));
        }

    }
}
