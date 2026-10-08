using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.Meta
{
    public enum StoryComparisonOperator
    {
        Equals,
        NotEquals,
        GreaterOrEqual,
        LessOrEqual
    }

    public abstract class MilestoneConditionSO : ScriptableObject
    {
        public abstract bool Evaluate(StoryProgressionService service);
    }

    [CreateAssetMenu(fileName = "FactCondition", menuName = "Zephyr/Progression/Conditions/Fact Condition")]
    public sealed class FactConditionSO : MilestoneConditionSO
    {
        [SerializeField] private StoryFactSO _fact;
        [SerializeField] private StoryComparisonOperator _operator = StoryComparisonOperator.Equals;
        [SerializeField] private bool _expectedBool = true;
        [SerializeField] private int _expectedInt;

        public StoryFactSO Fact => _fact;

        public override bool Evaluate(StoryProgressionService service)
        {
            if (service == null || _fact == null) return false;
            if (_fact.ValueType == StoryFactValueType.Bool)
            {
                bool actual = service.GetBool(_fact);
                return Compare(actual, _expectedBool, _operator);
            }

            int value = service.GetInt(_fact);
            return Compare(value, _expectedInt, _operator);
        }

        private static bool Compare(bool actual, bool expected, StoryComparisonOperator op)
        {
            switch (op)
            {
                case StoryComparisonOperator.Equals: return actual == expected;
                case StoryComparisonOperator.NotEquals: return actual != expected;
                default: return false;
            }
        }

        private static bool Compare(int actual, int expected, StoryComparisonOperator op)
        {
            switch (op)
            {
                case StoryComparisonOperator.Equals: return actual == expected;
                case StoryComparisonOperator.NotEquals: return actual != expected;
                case StoryComparisonOperator.GreaterOrEqual: return actual >= expected;
                case StoryComparisonOperator.LessOrEqual: return actual <= expected;
                default: return false;
            }
        }
    }

    public abstract class CompositeMilestoneConditionSO : MilestoneConditionSO
    {
        [SerializeField] private List<MilestoneConditionSO> _conditions = new List<MilestoneConditionSO>();

        protected IReadOnlyList<MilestoneConditionSO> Conditions => _conditions;
    }

    [CreateAssetMenu(fileName = "AllConditions", menuName = "Zephyr/Progression/Conditions/All Conditions")]
    public sealed class AllConditionsSO : CompositeMilestoneConditionSO
    {
        public override bool Evaluate(StoryProgressionService service)
        {
            if (Conditions.Count == 0) return false;
            for (int i = 0; i < Conditions.Count; i++)
            {
                if (Conditions[i] == null || !Conditions[i].Evaluate(service)) return false;
            }

            return true;
        }
    }

    [CreateAssetMenu(fileName = "AnyCondition", menuName = "Zephyr/Progression/Conditions/Any Condition")]
    public sealed class AnyConditionSO : CompositeMilestoneConditionSO
    {
        public override bool Evaluate(StoryProgressionService service)
        {
            for (int i = 0; i < Conditions.Count; i++)
            {
                if (Conditions[i] != null && Conditions[i].Evaluate(service)) return true;
            }

            return false;
        }
    }

    [CreateAssetMenu(fileName = "NotCondition", menuName = "Zephyr/Progression/Conditions/Not Condition")]
    public sealed class NotConditionSO : MilestoneConditionSO
    {
        [SerializeField] private MilestoneConditionSO _condition;

        public override bool Evaluate(StoryProgressionService service)
            => _condition != null && !_condition.Evaluate(service);
    }

    [CreateAssetMenu(fileName = "MilestoneCompletedCondition", menuName = "Zephyr/Progression/Conditions/Milestone Completed")]
    public sealed class MilestoneCompletedConditionSO : MilestoneConditionSO
    {
        [SerializeField] private MilestoneSO _milestone;

        public override bool Evaluate(StoryProgressionService service)
            => service != null && _milestone != null && service.IsCompleted(_milestone);
    }
}
