using System.Globalization;

namespace Edp.Workflow.Domain;

public interface IGuardEvaluator
{
    bool Evaluate(TransitionGuard? guard, IReadOnlyDictionary<string, object?> variables);
}

public sealed class GuardEvaluator : IGuardEvaluator
{
    public bool Evaluate(TransitionGuard? guard, IReadOnlyDictionary<string, object?> variables)
    {
        if (guard is null)
            return true;

        if (guard.Children is { Count: > 0 })
        {
            var results = guard.Children.Select(child => Evaluate(child, variables));
            return guard.LogicalOperator == GuardLogicalOperator.And
                ? results.All(result => result)
                : results.Any(result => result);
        }

        if (guard.Condition is null)
            return true;

        variables.TryGetValue(guard.Condition.Variable, out var actualValue);
        return EvaluateCondition(guard.Condition, actualValue);
    }

    private static bool EvaluateCondition(GuardCondition condition, object? actualValue)
    {
        return condition.Operator switch
        {
            GuardOperator.IsNull => actualValue is null,
            GuardOperator.IsNotNull => actualValue is not null,
            GuardOperator.Equals => Compare(actualValue, condition.ExpectedValue) == 0,
            GuardOperator.NotEquals => Compare(actualValue, condition.ExpectedValue) != 0,
            GuardOperator.GreaterThan => Compare(actualValue, condition.ExpectedValue) > 0,
            GuardOperator.LessThan => Compare(actualValue, condition.ExpectedValue) < 0,
            GuardOperator.GreaterThanOrEquals => Compare(actualValue, condition.ExpectedValue) >= 0,
            GuardOperator.LessThanOrEquals => Compare(actualValue, condition.ExpectedValue) <= 0,
            GuardOperator.Contains => actualValue?.ToString()?.Contains(condition.ExpectedValue?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,
            GuardOperator.StartsWith => actualValue?.ToString()?.StartsWith(condition.ExpectedValue?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,
            GuardOperator.EndsWith => actualValue?.ToString()?.EndsWith(condition.ExpectedValue?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,
            _ => throw new GuardEvaluationException(condition.Variable, $"Unsupported operator '{condition.Operator}'")
        };
    }

    private static int Compare(object? actualValue, object? expectedValue)
    {
        if (actualValue is null || expectedValue is null)
            return actualValue is null && expectedValue is null ? 0 : actualValue is null ? -1 : 1;

        if (decimal.TryParse(actualValue.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var actualNumber) &&
            decimal.TryParse(expectedValue.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var expectedNumber))
            return actualNumber.CompareTo(expectedNumber);

        return string.Compare(actualValue.ToString(), expectedValue.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
