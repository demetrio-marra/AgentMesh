using System.Net;
using System.Net.Http.Json;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgentMesh.Application.Tests;

public sealed class AppInstanceTests
{
    [Fact]
    public async Task ProcessRequest_UsesOnlyTheConversationProvidedForTheExecution()
    {
        var chatPipeline = new RecordingChatPipeline();
        var appInstance = CreateAppInstance(services => services.AddScoped<IChatRequestPipeline>(_ => chatPipeline));
        var firstConversation = new[] { Message("first") };
        var secondConversation = new[] { Message("second") };

        await appInstance.ProcessRequest("first request", firstConversation);
        await appInstance.ProcessRequest("second request", secondConversation);

        Assert.Equal(new[] { "first", "second" }, chatPipeline.ConversationTexts);
    }

    [Fact]
    public async Task SummarizeAsync_UsesTheSingleRegisteredPipeline()
    {
        var summarizationPipeline = new RecordingSummarizationPipeline();
        var appInstance = CreateAppInstance(services => services.AddScoped<ISummarizationPipeline>(_ => summarizationPipeline));

        var result = await appInstance.SummarizeAsync("en", new[] { Message("message") });

        Assert.Equal("summary", result.SummarizedContent);
        Assert.Equal("en", summarizationPipeline.Language);
        Assert.Equal(new[] { "message" }, summarizationPipeline.ConversationTexts);
    }

    [Fact]
    public async Task SummarizeAsync_RejectsZeroOrMultiplePipelines()
    {
        var noPipelinesAppInstance = CreateAppInstance();
        var noPipelinesException = await Assert.ThrowsAsync<PipelineRoutingException>(
            () => noPipelinesAppInstance.SummarizeAsync("en", Array.Empty<ContextMessage>()));
        Assert.Equal(503, noPipelinesException.StatusCode);

        var multiplePipelinesAppInstance = CreateAppInstance(services =>
        {
            services.AddScoped<ISummarizationPipeline, RecordingSummarizationPipeline>();
            services.AddScoped<ISummarizationPipeline, RecordingSummarizationPipeline>();
        });
        var multiplePipelinesException = await Assert.ThrowsAsync<PipelineRoutingException>(
            () => multiplePipelinesAppInstance.SummarizeAsync("en", Array.Empty<ContextMessage>()));
        Assert.Equal(503, multiplePipelinesException.StatusCode);
    }

    [Fact]
    public async Task AsyncExecutions_ReturnRequestIdsBeforeCompletingAndPostTerminalPayloads()
    {
        var callbackHandler = new CapturingHttpMessageHandler();
        var chatPipeline = new BlockingChatPipeline();
        var summarizationPipeline = new BlockingSummarizationPipeline();
        var appInstance = CreateAppInstance(
            services =>
            {
                services.AddScoped<IChatRequestPipeline>(_ => chatPipeline);
                services.AddScoped<ISummarizationPipeline>(_ => summarizationPipeline);
            },
            callbackHandler);

        var chatRequestId = appInstance.ProcessRequestAsync(
            "message",
            Array.Empty<ContextMessage>(),
            pipelineName: null,
            workflowStartedCallbackUrl: null,
            workflowStepStartedCallbackUrl: null,
            workflowStepCompletedCallbackUrl: null,
            workflowCompletedCallbackUrl: "https://callback.test/chat",
            workflowErrorCallbackUrl: "https://callback.test/chat-error");
        var summarizationRequestId = appInstance.SummarizeInBackground(
            "en",
            Array.Empty<ContextMessage>(),
            workflowStartedCallbackUrl: null,
            workflowStepStartedCallbackUrl: null,
            workflowStepCompletedCallbackUrl: null,
            workflowCompletedCallbackUrl: "https://callback.test/summarization",
            workflowErrorCallbackUrl: "https://callback.test/summarization-error");

        Assert.NotEqual(Guid.Empty, chatRequestId);
        Assert.NotEqual(Guid.Empty, summarizationRequestId);
        Assert.False(chatPipeline.Completed.Task.IsCompleted);
        Assert.False(summarizationPipeline.Completed.Task.IsCompleted);

        chatPipeline.AllowCompletion.SetResult();
        summarizationPipeline.AllowCompletion.SetResult();

        var callbacks = await callbackHandler.WaitForCallbacksAsync(2);
        Assert.Contains(callbacks, callback => callback.Url == "https://callback.test/chat" && callback.Body.Contains(chatRequestId.ToString(), StringComparison.Ordinal));
        Assert.Contains(callbacks, callback => callback.Url == "https://callback.test/summarization" && callback.Body.Contains(summarizationRequestId.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public void ProcessRequestAsync_RejectsRoutingFailureBeforeReturningARequestId()
    {
        var appInstance = CreateAppInstance();

        Assert.Throws<PipelineRoutingException>(() => appInstance.ProcessRequestAsync(
            "message",
            Array.Empty<ContextMessage>(),
            pipelineName: null,
            workflowStartedCallbackUrl: null,
            workflowStepStartedCallbackUrl: null,
            workflowStepCompletedCallbackUrl: null,
            workflowCompletedCallbackUrl: null,
            workflowErrorCallbackUrl: null));
    }

    [Fact]
    public async Task AsyncExecutions_PostTerminalErrorPayloadsWhenPipelinesFail()
    {
        var callbackHandler = new CapturingHttpMessageHandler();
        var appInstance = CreateAppInstance(
            services =>
            {
                services.AddScoped<IChatRequestPipeline, FailingChatPipeline>();
                services.AddScoped<ISummarizationPipeline, FailingSummarizationPipeline>();
            },
            callbackHandler);

        var chatRequestId = appInstance.ProcessRequestAsync(
            "message",
            Array.Empty<ContextMessage>(),
            pipelineName: null,
            workflowStartedCallbackUrl: null,
            workflowStepStartedCallbackUrl: null,
            workflowStepCompletedCallbackUrl: null,
            workflowCompletedCallbackUrl: null,
            workflowErrorCallbackUrl: "https://callback.test/chat-error");
        var summarizationRequestId = appInstance.SummarizeInBackground(
            "en",
            Array.Empty<ContextMessage>(),
            workflowStartedCallbackUrl: null,
            workflowStepStartedCallbackUrl: null,
            workflowStepCompletedCallbackUrl: null,
            workflowCompletedCallbackUrl: null,
            workflowErrorCallbackUrl: "https://callback.test/summarization-error");

        var callbacks = await callbackHandler.WaitForCallbacksAsync(2);

        Assert.Contains(callbacks, callback => callback.Url == "https://callback.test/chat-error" && callback.Body.Contains(chatRequestId.ToString(), StringComparison.Ordinal));
        Assert.Contains(callbacks, callback => callback.Url == "https://callback.test/summarization-error" && callback.Body.Contains(summarizationRequestId.ToString(), StringComparison.Ordinal));
    }

    private static AppInstance CreateAppInstance(Action<IServiceCollection>? configureServices = null, HttpMessageHandler? handler = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<CallbackNotifierContext>();
        configureServices?.Invoke(services);
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = new TestHttpClientFactory(handler ?? new CapturingHttpMessageHandler());

        return new AppInstance(
            serviceProvider,
            Array.Empty<AgentFlatConfigurationRecord>(),
            new PluginHostState(),
            httpClientFactory,
            NullLogger<AppInstance>.Instance);
    }

    private static ContextMessage Message(string text) => new()
    {
        Role = ContextMessageRole.User,
        Date = DateTime.UtcNow,
        Text = text
    };

    private sealed class RecordingChatPipeline : IChatRequestPipeline
    {
        public List<string> ConversationTexts { get; } = [];
        public string Name => "chat";
        public string FinalResponse => "response";

        public void SetParameterInitialValues(string userLastRequest, IEnumerable<ContextMessage> initialChatHistory, DateTime requestDateTime)
        {
            ConversationTexts.AddRange(initialChatHistory.Select(message => message.Text));
        }

        public Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<EWStepStatisticsRecord>>([]);
    }

    private sealed class RecordingSummarizationPipeline : ISummarizationPipeline
    {
        public string Language { get; private set; } = string.Empty;
        public List<string> ConversationTexts { get; } = [];
        public string SummarizedContent => "summary";
        public DateTime SummarizedContentDatetime => DateTime.UnixEpoch;

        public void SetParameterInitialValues(string summarizationLanguage, IEnumerable<ContextMessage> chatMessagesToSummarize, DateTime requestDateTime)
        {
            Language = summarizationLanguage;
            ConversationTexts.AddRange(chatMessagesToSummarize.Select(message => message.Text));
        }

        public Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<EWStepStatisticsRecord>>([]);
    }

    private sealed class BlockingChatPipeline : IChatRequestPipeline
    {
        public TaskCompletionSource AllowCompletion { get; } = new();
        public TaskCompletionSource Completed { get; } = new();
        public string Name => "chat";
        public string FinalResponse => "response";

        public void SetParameterInitialValues(string userLastRequest, IEnumerable<ContextMessage> initialChatHistory, DateTime requestDateTime) { }

        public async Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await AllowCompletion.Task.WaitAsync(cancellationToken);
            Completed.SetResult();
            return [];
        }
    }

    private sealed class BlockingSummarizationPipeline : ISummarizationPipeline
    {
        public TaskCompletionSource AllowCompletion { get; } = new();
        public TaskCompletionSource Completed { get; } = new();
        public string SummarizedContent => "summary";
        public DateTime SummarizedContentDatetime => DateTime.UnixEpoch;

        public void SetParameterInitialValues(string summarizationLanguage, IEnumerable<ContextMessage> chatMessagesToSummarize, DateTime requestDateTime) { }

        public async Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await AllowCompletion.Task.WaitAsync(cancellationToken);
            Completed.SetResult();
            return [];
        }
    }

    private sealed class FailingChatPipeline : IChatRequestPipeline
    {
        public string Name => "chat";
        public string FinalResponse => string.Empty;

        public void SetParameterInitialValues(string userLastRequest, IEnumerable<ContextMessage> initialChatHistory, DateTime requestDateTime) { }

        public Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Chat pipeline failed.");
    }

    private sealed class FailingSummarizationPipeline : ISummarizationPipeline
    {
        public string SummarizedContent => string.Empty;
        public DateTime SummarizedContentDatetime => DateTime.UnixEpoch;

        public void SetParameterInitialValues(string summarizationLanguage, IEnumerable<ContextMessage> chatMessagesToSummarize, DateTime requestDateTime) { }

        public Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Summarization pipeline failed.");
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<(string Url, string Body)> _callbacks = [];
        private readonly TaskCompletionSource _receivedCallbacks = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            lock (_callbacks)
            {
                _callbacks.Add((request.RequestUri!.ToString(), body));
                if (_callbacks.Count >= 2)
                {
                    _receivedCallbacks.TrySetResult();
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public async Task<IReadOnlyList<(string Url, string Body)>> WaitForCallbacksAsync(int expectedCount)
        {
            await _receivedCallbacks.Task.WaitAsync(TimeSpan.FromSeconds(5));
            lock (_callbacks)
            {
                Assert.Equal(expectedCount, _callbacks.Count);
                return _callbacks.ToList();
            }
        }
    }
}
