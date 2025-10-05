namespace Yina.Common.Protocols;

/// <summary>Marker interface for transportable messages.</summary>
public interface IMessage
{
    /// <summary>Gets the human-readable name for the message.</summary>
    string Name { get; }
}
