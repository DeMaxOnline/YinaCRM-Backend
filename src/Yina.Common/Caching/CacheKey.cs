using System;
using System.Collections.Generic;
using System.Text;

namespace Yina.Common.Caching;

/// <summary>
/// Helper to build consistent hierarchical cache keys (e.g. <c>clients:123:profile</c>).
/// </summary>
public sealed class CacheKey
{
    private readonly List<string> _parts = new(8);

    public CacheKey Add(string part)
    {
        if (!string.IsNullOrWhiteSpace(part))
        {
            _parts.Add(part.Trim());
        }

        return this;
    }

    public CacheKey Add(params string[] parts)
    {
        if (parts is null)
        {
            return this;
        }

        foreach (var p in parts)
        {
            Add(p);
        }

        return this;
    }

    public override string ToString()
    {
        if (_parts.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < _parts.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(':');
            }

            sb.Append(_parts[i]);
        }

        return sb.ToString();
    }
}
