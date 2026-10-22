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

        [Test]
        public void RuntimeStateRestoresRunLayersAndPotentialWithoutTemporaryModifiers()
        {
            _stats.SetBase(StatType.Attack, 20f);
            _stats.SetRunPotential(StatType.Attack, 4.5f);
            _stats.ApplyPotentialScaledGain(StatType.Attack, 10f);
            _stats.AddRunGainPercent(StatType.Attack, 0.15f);
            _stats.AddModifier(StatType.Attack, 3f);
            _stats.AddModifierPercent(StatType.Attack, 0.1f);
            _stats.AddCategoryMultiplier("Melee", 0.2f);
            _stats.AddTemporaryModifier(StatType.Attack, 50f);

            StatsCore.RuntimeState snapshot = _stats.CaptureRuntimeState();
            GameObject destination = new GameObject("StatsCoreRuntimeStateDestination");
            try
            {
                StatsCore restored = destination.AddComponent<StatsCore>();
                restored.SetBase(StatType.Attack, 20f);
                restored.RestoreRuntimeState(snapshot);

                Assert.That(restored.GetPotential(StatType.Attack), Is.EqualTo(4.5f).Within(0.001f));
                Assert.That(restored.GetStatValue(StatType.Attack), Is.EqualTo(34.375f).Within(0.001f));
                Assert.That(restored.GetCategoryMultiplier("Melee"), Is.EqualTo(0.2f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(destination);
            }
        }

    }
}
