using System.Diagnostics;

namespace Yina.Common.Diagnostics;

public static class ActivityConventions
{
    public const string SourceName = "yinacrm.common";

    public static readonly ActivitySource Source = new(SourceName);

    public static class Keys
    {
        public const string CorrelationId = "correlation.id";
        public const string CausationId = "causation.id";
        public const string UserId = "user.id";
        public const string MessageName = "message.name";
        public const string MessageType = "message.type";
        public const string Success = "success";
        public const string ErrorCode = "error.code";
        public const string ErrorMessage = "error.message";
    }

    public static string Command(string name) => $"cmd:{name}";

    public static string Query(string name) => $"qry:{name}";

    public static string Event(string name) => $"evt:{name}";

    public static Activity? Start(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        string? messageName = null,
        string? userId = null)
    {
        var activity = Source.StartActivity(name, kind);
        if (activity is null)
        {
            return null;
        }

        var correlationId = Correlation.EnsureCorrelationId();
        activity.SetTag(Keys.CorrelationId, correlationId);

        var causation = Correlation.GetCausationId();
        if (!string.IsNullOrWhiteSpace(causation))
        {
            activity.SetTag(Keys.CausationId, causation);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            activity.SetTag(Keys.UserId, userId);
        }

        if (!string.IsNullOrWhiteSpace(messageName))
        {
            activity.SetTag(Keys.MessageName, messageName);
        }

        return activity;
    }
}
