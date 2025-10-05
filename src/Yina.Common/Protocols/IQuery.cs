namespace Yina.Common.Protocols;

/// <summary>Represents a query message producing results.</summary>
public interface IQuery<TResult> : IMessage
{
}

/// <summary>Marker interface for queries returning results (non-generic).</summary>
public interface IQuery : IMessage
{
}


