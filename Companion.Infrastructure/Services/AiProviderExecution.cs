using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace Companion.Infrastructure.Services;

internal static class AiProviderExecution
{
    public static async Task<AiProviderExecutionResult> SendAsync(
        HttpClient httpClient,
        HttpRequestMessage request,
        string providerName,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 1)));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await httpClient.SendAsync(request, timeoutCts.Token);
            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    BuildErrorMessage(providerName, response.StatusCode, body),
                    null,
                    response.StatusCode);
            }

            stopwatch.Stop();

            return new AiProviderExecutionResult(body, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"{providerName} provider timed out after {Math.Max(timeoutSeconds, 1)} second(s).", ex);
        }
    }

    public static HttpRequestMessage CreateJsonRequest<TPayload>(
        HttpMethod method,
        string baseUrl,
        string relativeUri,
        TPayload payload,
        JsonSerializerOptions jsonOptions)
    {
        var request = new HttpRequestMessage(
            method,
            new Uri(new Uri($"{baseUrl.Trim().TrimEnd('/')}/"), relativeUri));
        request.Headers.ConnectionClose = true;
        request.Content = JsonContent.Create(payload, options: jsonOptions);
        return request;
    }

    private static string BuildErrorMessage(string providerName, System.Net.HttpStatusCode statusCode, string body)
    {
        var compactBody = string.IsNullOrWhiteSpace(body) ? null : body.Trim();

        if (string.IsNullOrWhiteSpace(compactBody))
        {
            return $"{providerName} provider returned {(int)statusCode} ({statusCode}).";
        }

        return $"{providerName} provider returned {(int)statusCode} ({statusCode}): {compactBody}";
    }
}

internal sealed record AiProviderExecutionResult(
    string Body,
    long LatencyMs);
