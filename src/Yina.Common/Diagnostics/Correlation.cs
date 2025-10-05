using System;
using System.Diagnostics;
using System.Threading;

namespace Yina.Common.Diagnostics;

/// <summary>Manages correlation/causation identifiers across async flows.</summary>
public static class Correlation
{
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();
    private static readonly AsyncLocal<string?> CurrentCausationId = new();

    public static string EnsureCorrelationId()
    {
        var id = GetCorrelationId();
        if (!string.IsNullOrWhiteSpace(id))
        {
            return id!;
        }

        var activityId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(activityId))
        {
            SetCorrelationId(activityId!);
            return activityId!;
        }

        var generated = Guid.NewGuid().ToString("N");
        SetCorrelationId(generated);
        return generated;
    }

    public static string? GetCorrelationId() => CurrentCorrelationId.Value;

    public static void SetCorrelationId(string? value)
        => CurrentCorrelationId.Value = string.IsNullOrWhiteSpace(value) ? null : value;

    public static string? GetCausationId() => CurrentCausationId.Value;

    public static void SetCausationId(string? value)
        => CurrentCausationId.Value = string.IsNullOrWhiteSpace(value) ? null : value;
}



