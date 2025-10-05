using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Configuration;

public interface IConfigurationValidator<in TOptions>
{
    Result Validate(string name, TOptions options);
}
