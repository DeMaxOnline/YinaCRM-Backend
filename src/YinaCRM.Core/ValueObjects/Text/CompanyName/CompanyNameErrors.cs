using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class CompanyNameErrors
{
    public static Error Empty() => Error.Create("COMPANYNAME_EMPTY", "Company name is required", 400);
    public static Error TooLong() => Error.Create("COMPANYNAME_TOO_LONG", "Company name must be at most 200 characters", 400);
}



