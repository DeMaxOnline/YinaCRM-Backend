using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Security;
using YinaCRM.Infrastructure.Abstractions.Webhooks;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Webhooks;

public sealed class HttpWebhookDispatcher : IWebhookDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISigningService _signingService;
    private readonly ILogger<HttpWebhookDispatcher> _logger;
    private readonly TimeProvider _timeProvider;

    public HttpWebhookDispatcher(
        IHttpClientFactory httpClientFactory,
        ISigningService signingService,
        ILogger<HttpWebhookDispatcher> logger,
        TimeProvider? timeProvider = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _signingService = signingService ?? throw new ArgumentNullException(nameof(signingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<WebhookDispatchResult>> DispatchAsync(WebhookDispatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var attempts = 0;
        var failureReasons = new List<string>();
        int? lastStatusCode = null;

        while (attempts < request.MaxAttempts)
        {
            attempts++;
            var outcome = await SendOnceAsync(request, cancellationToken).ConfigureAwait(false);
            if (outcome.IsSuccess)
            {
                return Result.Success(new WebhookDispatchResult(
                    Delivered: true,
                    Attempts: attempts,
                    LastStatusCode: outcome.StatusCode,
                    DeliveredAtUtc: outcome.DeliveredAtUtc,
                    FailureReasons: failureReasons));
            }

            lastStatusCode = outcome.StatusCode;
            failureReasons.Add(outcome.ErrorMessage ?? "Unknown error.");

            if (attempts >= request.MaxAttempts)
            {
                break;
            }

            var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempts - 1));
            _logger.LogWarning(
                "Webhook delivery attempt {Attempt} failed for {Endpoint}. Retrying in {Delay}.",
                attempts,
                request.Endpoint,
                delay);

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(new WebhookDispatchResult(
            Delivered: false,
            Attempts: attempts,
            LastStatusCode: lastStatusCode,
            DeliveredAtUtc: null,
            FailureReasons: failureReasons));
    }

    private async Task<AttemptOutcome> SendOnceAsync(WebhookDispatchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("webhooks");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.Endpoint)
            {
                Content = new StringContent(request.Payload, Encoding.UTF8, "application/json"),
            };

            foreach (var header in request.Headers)
            {
                if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    httpRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Secret))
            {
                var signResult = await _signingService.SignAsync(new SignRequest(
                    request.TenantId,
                    request.Payload,
                    WebhookSignatureAlgorithm,
                    null,
                    request.Headers), cancellationToken).ConfigureAwait(false);

                if (signResult.IsFailure)
                {
                    return AttemptOutcome.CreateFailure(null, signResult.Error.Message);
                }

                httpRequest.Headers.Add("X-Yina-Signature", signResult.Value);
            }

            var response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                return AttemptOutcome.CreateSuccess(statusCode, _timeProvider.GetUtcNow());
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                "Webhook delivery failed with status {StatusCode}: {Body}",
                statusCode,
                body);
            return AttemptOutcome.CreateFailure(statusCode, $"Webhook delivery failed with status {statusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Webhook delivery failed due to HTTP error.");
            return AttemptOutcome.CreateFailure(null, ex.Message);
        }
    }

    private const string WebhookSignatureAlgorithm = "sha256";

    private readonly record struct AttemptOutcome(bool IsSuccess, int? StatusCode, DateTimeOffset? DeliveredAtUtc, string? ErrorMessage)
    {
        public static AttemptOutcome CreateSuccess(int statusCode, DateTimeOffset deliveredAtUtc)
            => new(true, statusCode, deliveredAtUtc, null);

        public static AttemptOutcome CreateFailure(int? statusCode, string errorMessage)
            => new(false, statusCode, null, errorMessage);
    }
}
