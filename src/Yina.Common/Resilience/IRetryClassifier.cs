using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Yina.Common.Abstractions.Errors;

namespace Yina.Common.Resilience;

public interface IRetryClassifier
{
    bool IsTransient(Exception ex);

    bool IsRetryable(Error error);
}

public sealed class DefaultRetryClassifier : IRetryClassifier
{
    public bool IsTransient(Exception ex)
    {
        return ex is TimeoutException
            || ex is TaskCanceledException
            || ex is OperationCanceledException
            || ex is SocketException
            || ex is IOException
            || (ex is HttpRequestException hre && (
                hre.StatusCode is HttpStatusCode.RequestTimeout
                    or HttpStatusCode.BadGateway
                    or HttpStatusCode.GatewayTimeout
                    or HttpStatusCode.ServiceUnavailable));
    }

    public bool IsRetryable(Error error) => error.IsRetryable();
}
