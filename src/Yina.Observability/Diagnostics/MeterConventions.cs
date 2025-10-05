using System.Diagnostics.Metrics;

namespace Yina.Observability.Diagnostics;

/// <summary>Common meter naming conventions for Yina services.</summary>
public static class MeterConventions
{
    public const string CommonMeterName = "yinacrm.common";

    public static readonly Meter CommonMeter = new(CommonMeterName);

    public static string Instrument(string name)
        => string.IsNullOrWhiteSpace(name) ? CommonMeterName : $"{CommonMeterName}.{name}";
}
