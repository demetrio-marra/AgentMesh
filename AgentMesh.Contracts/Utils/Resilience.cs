using AgentMesh.Application.Configuration;
using AgentMesh.Application.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net;
using System.Net.Sockets;

namespace AgentMesh.Application.Utils
{
    public class Resilience(ResilienceConfiguration configuration)
    {
        private readonly ResilienceConfiguration _configuration = configuration;

        public Task<T> AgentRunWithRetryAsync<T>(Func<Task<T>> action, string agentName, ILogger? logger = null, int? retryCount = null)
        {
            var effectiveRetryCount = retryCount ?? _configuration.RetryCount;
            var jitterer = new Random();
            var policy = Policy.Handle<BadStructuredResponseException>().Or<EmptyAgentResponseException>()
                .Or<Exception>(ex => ex.GetType().Name == "ClientResultException" && ex.Message.Contains("Tool choice is none, but model called a tool"))
                .Or<Exception>(ex => ex.GetType().Name == "ClientResultException" && ex.Message.Contains("Service unavailable"))
                .Or<Exception>(ex => ex.GetType().Name == "ClientResultException" && (ex.Message.Contains("Internal server error", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("status code 500", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("(500)", StringComparison.OrdinalIgnoreCase)))
                .WaitAndRetryAsync(effectiveRetryCount, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 500)),
                    (exception, timeSpan, currentRetryCount, _) => logger?.LogWarning(exception, "Retry {RetryCount} for agent {AgentName} after {DelaySeconds:F1}s due to error: {ErrorMessage}", currentRetryCount, agentName, timeSpan.TotalSeconds, exception.Message));

            return policy.ExecuteAsync(action);
        }

        public Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> action, string operationName, ILogger? logger = null, int? retryCount = null)
        {
            var effectiveRetryCount = retryCount ?? _configuration.RetryCount;
            var jitterer = new Random();
            var policy = Policy.Handle<HttpRequestException>().Or<TaskCanceledException>().Or<TimeoutException>().Or<Exception>(IsHostNotFoundException)
                .OrResult<HttpResponseMessage>(response => (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout || response.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(effectiveRetryCount,
                    (attempt, outcome, _) => outcome.Result?.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 500)),
                    (outcome, timeSpan, attempt, _) =>
                    {
                        var reason = outcome.Exception?.Message ?? $"HTTP {(int)outcome.Result.StatusCode} {outcome.Result.StatusCode}";
                        logger?.LogWarning(outcome.Exception, "Retry {RetryCount} for {OperationName} after {DelaySeconds:F1}s due to: {Reason}", attempt, operationName, timeSpan.TotalSeconds, reason);
                        return Task.CompletedTask;
                    });

            return policy.ExecuteAsync(action);
        }

        private static bool IsHostNotFoundException(Exception exception)
        {
            if (exception is SocketException socketException)
            {
                return socketException.SocketErrorCode is SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain;
            }

            if (exception is HttpRequestException httpRequestException && httpRequestException.InnerException is SocketException innerSocketException)
            {
                return innerSocketException.SocketErrorCode is SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain;
            }

            return exception.Message.Contains("host not found", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("no such host", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("name or service not known", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("temporary failure in name resolution", StringComparison.OrdinalIgnoreCase);
        }
    }
}