using System;
using System.Collections.Generic;

namespace Yina.Common.Foundation.Pagination;

public sealed class Page<T>
{
    public Page(IReadOnlyList<T> items, long totalCount, int pageNumber, int pageSize)
    {
        Items = items ?? Array.Empty<T>();
        TotalCount = totalCount < 0 ? 0 : totalCount;
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? 1 : pageSize;
    }

    public IReadOnlyList<T> Items { get; }

    public long TotalCount { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    public static Page<T> Empty(int pageNumber, int pageSize)
        => new(Array.Empty<T>(), 0, pageNumber, pageSize);
}
