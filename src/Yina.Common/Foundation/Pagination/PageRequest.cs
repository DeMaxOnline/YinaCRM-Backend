using System;

namespace Yina.Common.Foundation.Pagination;

public readonly record struct PageRequest(int PageNumber, int PageSize)
{
    public const int DefaultPageNumber = 1;

    public const int DefaultPageSize = 25;

    public const int MaxPageSize = 500;

    public static PageRequest Default => new(DefaultPageNumber, DefaultPageSize);

    public static PageRequest Create(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "PageNumber must be at least 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be at least 1.");
        }

        if (pageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"PageSize must be <= {MaxPageSize}.");
        }

        return new(pageNumber, pageSize);
    }

    public int Skip => (PageNumber - 1) * PageSize;

    public int Take => PageSize;
}
