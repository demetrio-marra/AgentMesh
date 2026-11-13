using System.Net;
using System.Text;
using System.Text.Json;
using AgentMesh.Contracts.Models.SystemOne;
using AgentMesh.Infrastructure.SystemOne;
using Xunit;

namespace AgentMesh.Infrastructure.SystemOne.Tests;

public class SystemOneServiceTests
{
    [Theory]
    [InlineData("not-an-absolute-url", "secret", "model", 5)]
    [InlineData("ftp://provider.test", "secret", "model", 5)]
    [InlineData("https://provider.test", "", "model", 5)]
    [InlineData("https://provider.test", "secret", " ", 5)]
    [InlineData("https://provider.test", "secret", "model", 0)]
    [InlineData("https://provider.test", "secret", "model", -1)]
    public void InvalidConfigurationFailsDuringConstructionBeforeHttpExecution(
        string baseUrl,
        string apiKey,
        string model,
        int timeoutSeconds)
    {
        var handler = new RecordingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var client = new HttpClient(handler);

        Assert.Throws<SystemOneConfigurationException>(() =>
            new SystemOneService(client, new SystemOneServiceConfiguration
            {
                BaseUrl = baseUrl,
                ApiKey = apiKey,
                Model = model,
                TimeoutSeconds = timeoutSeconds
            }));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public void ValidConfigurationConstructsStandaloneServiceWithoutSending()
    {
        var handler = new RecordingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var client = new HttpClient(handler);

        var service = new SystemOneService(client, CreateConfiguration());

        Assert.NotNull(service);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task ChoiceAndScoreCriteriaAcceptTheirExactBoundaryCounts()
    {
        var handler = new RecordingHandler(CreateSuccessResponseAsync);
        using var client = new HttpClient(handler);
        var service = new SystemOneService(client, CreateConfiguration());

        foreach (var choiceCount in new[] { 1, 255 })
        {
            var criteria = Enumerable.Range(0, choiceCount)
                .ToDictionary(index => $"choice{index}", index => SystemOneJsonValue.From($"description{index}"), StringComparer.Ordinal);
            await service.EvaluateAsync(CreateInput(new SystemOneChoiceQuestion { Criteria = criteria }));
        }

        foreach (var scoreCount in new[] { 1, 10 })
        {
            var criteria = Enumerable.Range(0, scoreCount)
                .Select(index => SystemOneJsonValue.From($"level{index}"))
                .ToArray();
            await service.EvaluateAsync(CreateInput(new SystemOneScoreQuestion { Criteria = criteria }));
        }

        Assert.Equal(4, handler.RequestCount);
    }

    [Fact]
    public async Task InvalidInputShapesAndUnsupportedFeaturesAreRejectedBeforeSending()
    {
        var handler = new RecordingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var client = new HttpClient(handler);
        var service = new SystemOneService(client, CreateConfiguration());

        var invalidInputs = new List<SystemOneInput>
        {
            CreateInput(new SystemOneNoulQuestion(), SystemOneJsonValue.From(12)),
            CreateInput(new SystemOneNoulQuestion(), SystemOneJsonValue.Null),
            CreateInput(new SystemOneNoulQuestion(), SystemOneJsonValue.From(true)),
            CreateInput(new SystemOneNoulQuestion { Instructions = SystemOneJsonValue.From(12) }),
            CreateInput(new SystemOneNoulQuestion
            {
                Criteria = new Dictionary<bool, SystemOneJsonValue> { [true] = SystemOneJsonValue.From(false) }
            }),
            CreateInput(new SystemOneChoiceQuestion { Criteria = new Dictionary<string, SystemOneJsonValue>() }),
            CreateInput(new SystemOneChoiceQuestion
            {
                Criteria = Enumerable.Range(0, 256)
                    .ToDictionary(index => $"choice{index}", _ => SystemOneJsonValue.From("description"))
            }),
            CreateInput(new SystemOneScoreQuestion()),
            CreateInput(new SystemOneScoreQuestion
            {
                Criteria = Enumerable.Range(0, 11).Select(index => SystemOneJsonValue.From($"level{index}")).ToArray()
            }),
            CreateInput(new UnsupportedQuestion())
        };

        foreach (var input in invalidInputs)
        {
            await Assert.ThrowsAsync<SystemOneValidationException>(() => service.EvaluateAsync(input));
        }

        var emptyQuestions = new SystemOneInput(
            SystemOneJsonValue.From("text"),
            new Dictionary<string, SystemOneQuestion>());
        await Assert.ThrowsAsync<SystemOneValidationException>(() => service.EvaluateAsync(emptyQuestions));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task NoulAllowsOptionalInstructionsAndNullCriteriaDescriptions()
    {
        var handler = new RecordingHandler(CreateSuccessResponseAsync);
        using var client = new HttpClient(handler);
        var service = new SystemOneService(client, CreateConfiguration());
        var input = CreateInput(new SystemOneNoulQuestion
        {
            Criteria = new Dictionary<bool, SystemOneJsonValue>
            {
                [true] = SystemOneJsonValue.Null,
                [false] = SystemOneJsonValue.From("no")
            }
        });

        await service.EvaluateAsync(input);

        using var request = JsonDocument.Parse(handler.RequestBodies.Single());
        var question = request.RootElement.GetProperty("questions").GetProperty("question");
        Assert.False(question.TryGetProperty("instructions", out _));
        Assert.Equal(JsonValueKind.Null, question.GetProperty("criteria").GetProperty("true").ValueKind);
        Assert.Equal("no", question.GetProperty("criteria").GetProperty("false").GetString());
    }

    [Fact]
    public async Task SendsOneAuthenticatedJsonPostToEachConfiguredProviderWithPreservedPrefixes()
    {
        var firstHandler = new RecordingHandler(CreateSuccessResponseAsync);
        var secondHandler = new RecordingHandler(CreateSuccessResponseAsync);
        using var firstClient = new HttpClient(firstHandler);
        using var secondClient = new HttpClient(secondHandler);
        var firstService = new SystemOneService(firstClient, new SystemOneServiceConfiguration
        {
            BaseUrl = "https://first.provider.test/api",
            ApiKey = "first-secret",
            Model = "first-model",
            TimeoutSeconds = 5
        });
        var secondService = new SystemOneService(secondClient, new SystemOneServiceConfiguration
        {
            BaseUrl = "https://second.provider.test/api/",
            ApiKey = "second-secret",
            Model = "second-model",
            TimeoutSeconds = 5
        });
        var input = new SystemOneInput(
            SystemOneJsonValue.Parse("""{"message":["nested",{"count":3}]}"""),
            new Dictionary<string, SystemOneQuestion>
            {
                ["noulQuestion"] = new SystemOneNoulQuestion
                {
                    Instructions = SystemOneJsonValue.Parse("""{"rule":"inspect"}""")
                },
                ["choiceQuestion"] = new SystemOneChoiceQuestion
                {
                    Instructions = SystemOneJsonValue.From("choose"),
                    Criteria = new Dictionary<string, SystemOneJsonValue>
                    {
                        ["userChoiceKey"] = SystemOneJsonValue.Parse("""["description",{"rank":1}]""")
                    }
                },
                ["scoreQuestion"] = new SystemOneScoreQuestion
                {
                    Instructions = SystemOneJsonValue.From(new[] { "score", "carefully" }),
                    Criteria = [SystemOneJsonValue.From("low")]
                }
            });

        await firstService.EvaluateAsync(input);
        await secondService.EvaluateAsync(input);

        AssertProtocolRequest(Assert.Single(firstHandler.Requests), "https://first.provider.test/api/v1/systemone", "first-secret", "first-model");
        AssertProtocolRequest(Assert.Single(secondHandler.Requests), "https://second.provider.test/api/v1/systemone", "second-secret", "second-model");
        Assert.Equal(1, firstHandler.RequestCount);
        Assert.Equal(1, secondHandler.RequestCount);
    }

    [Fact]
    public async Task MapsTypedAnswersActualModelAndRequiredUsageWithoutRounding()
    {
        var handler = new RecordingHandler((_, _) => Task.FromResult(JsonResponse(ValidMixedResponse)));
        using var client = new HttpClient(handler);
        var service = new SystemOneService(client, CreateConfiguration());

        var result = await service.EvaluateAsync(CreateMixedInput());

        Assert.Equal("jev-1.14.0", result.Model);
        Assert.Equal(120, result.Usage.InputTokens);
        Assert.Equal(12, result.Usage.OutputTokens);
        Assert.Equal(0.98, Assert.IsType<SystemOneNoulAnswer>(result.Answers["noulQuestion"]).YesProbability);
        var choice = Assert.IsType<SystemOneChoiceAnswer>(result.Answers["choiceQuestion"]);
        Assert.Equal("yes", choice.Choice);
        Assert.Equal(0.7, choice.Confidence);
        Assert.Equal(0.3, choice.Probabilities["no"]);
        var score = Assert.IsType<SystemOneScoreAnswer>(result.Answers["scoreQuestion"]);
        Assert.Equal(1.7, score.Score);
        Assert.Equal(0.55, score.Confidence);
        Assert.Equal(JsonValueKind.Object, score.Legend["1"].Value.ValueKind);
        Assert.Equal(0.7, score.Probabilities["2"]);
    }

    [Fact]
    public async Task ZeroUsageIsPreservedWithoutLocalEstimation()
    {
        var handler = new RecordingHandler((_, _) => Task.FromResult(JsonResponse("""
            {"model":"actual-model","answers":{"question":{"type":"noul","noul":0.5}},"usage":{"input_tokens":0,"output_tokens":0}}
            """)));
        using var client = new HttpClient(handler);
        var service = new SystemOneService(client, CreateConfiguration());

        var result = await service.EvaluateAsync(CreateInput(new SystemOneNoulQuestion()));

        Assert.Equal(0, result.Usage.InputTokens);
        Assert.Equal(0, result.Usage.OutputTokens);
    }

    [Fact]
    public async Task ConcurrentEvaluationsKeepResponseModelsAnswersAndUsageIndependent()
    {
        var handler = new RecordingHandler(async (request, cancellationToken) =>
        {
            using var requestBody = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var state = requestBody.RootElement.GetProperty("state").GetString()!;
            var questionName = Assert.Single(requestBody.RootElement.GetProperty("questions").EnumerateObject()).Name;
            await Task.Delay(state == "first" ? 50 : 1, cancellationToken);
            return JsonResponse(JsonSerializer.Serialize(new
            {
                model = $"actual-{state}",
                answers = new Dictionary<string, object>
                {
                    [questionName] = new { type = "noul", noul = state == "first" ? 0.25 : 0.75 }
                },
                usage = new { input_tokens = state == "first" ? 11 : 22, output_tokens = state == "first" ? 1 : 2 }
            }));
        });
        using var client = new HttpClient(handler);
        var service = new SystemOneService(client, CreateConfiguration());
        var firstInput = new SystemOneInput(
            SystemOneJsonValue.From("first"),
            new Dictionary<string, SystemOneQuestion> { ["firstQuestion"] = new SystemOneNoulQuestion() });
        var secondInput = new SystemOneInput(
            SystemOneJsonValue.From("second"),
            new Dictionary<string, SystemOneQuestion> { ["secondQuestion"] = new SystemOneNoulQuestion() });

        var results = await Task.WhenAll(
            service.EvaluateAsync(firstInput),
            service.EvaluateAsync(secondInput));

        Assert.Equal("actual-first", results[0].Model);
        Assert.Equal(0.25, Assert.IsType<SystemOneNoulAnswer>(results[0].Answers["firstQuestion"]).YesProbability);
        Assert.Equal(new SystemOneUsage(11, 1), results[0].Usage);
        Assert.Equal("actual-second", results[1].Model);
        Assert.Equal(0.75, Assert.IsType<SystemOneNoulAnswer>(results[1].Answers["secondQuestion"]).YesProbability);
        Assert.Equal(new SystemOneUsage(22, 2), results[1].Usage);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task InvalidResponseSemanticsAreRejected()
    {
        var invalidResponses = new[]
        {
            ValidMixedResponse.Replace("\"noul\":0.98", "\"noul\":1.01", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"confidence\":0.7", "\"confidence\":1.01", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"choice\":\"yes\"", "\"choice\":\"missing\"", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"no\":0.3", "\"other\":0.3", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"score\":1.7", "\"score\":2.1", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"confidence\":0.55", "\"confidence\":-0.1", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"2\":0.7", "\"2\":1.2", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"legend\":{\"0\":\"low\",\"1\":{\"label\":\"medium\"},\"2\":[\"high\"]}",
                "\"legend\":{\"0\":\"low\",\"1\":{\"label\":\"medium\"}}", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"noul\":0.98", "\"noul\":1e400", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"type\":\"noul\"", "\"type\":\"box\"", StringComparison.Ordinal),
            ValidMixedResponse.Replace("\"noulQuestion\"", "\"unexpectedQuestion\"", StringComparison.Ordinal)
        };

        foreach (var responseBody in invalidResponses)
        {
            var handler = new RecordingHandler((_, _) => Task.FromResult(JsonResponse(responseBody)));
            using var client = new HttpClient(handler);
            var service = new SystemOneService(client, CreateConfiguration());

            await Assert.ThrowsAsync<SystemOneResponseException>(() => service.EvaluateAsync(CreateMixedInput()));
        }
    }

    [Fact]
    public async Task HttpStatusesAreExplicitSingleSendFailuresAndValidationDetailsAreSanitized()
    {
        var cases = new[]
        {
            (Status: HttpStatusCode.BadRequest, Body: """{"detail":"invalid privateStateValue using secret-key"}""", HasDetails: true),
            (Status: HttpStatusCode.Unauthorized, Body: """{"detail":"secret-key"}""", HasDetails: false),
            (Status: HttpStatusCode.UnprocessableEntity, Body: """{"detail":[{"loc":["body","questions","secretQuestionName"],"msg":"invalid privateStateValue secret-key","type":"value_error"}]}""", HasDetails: true),
            (Status: HttpStatusCode.TooManyRequests, Body: """{"detail":"secret-key"}""", HasDetails: false),
            (Status: HttpStatusCode.InternalServerError, Body: """{"detail":"secret-key"}""", HasDetails: false)
        };
        var input = new SystemOneInput(
            SystemOneJsonValue.From("privateStateValue"),
            new Dictionary<string, SystemOneQuestion>
            {
                ["secretQuestionName"] = new SystemOneNoulQuestion()
            });

        foreach (var item in cases)
        {
            var handler = new RecordingHandler((_, _) => Task.FromResult(new HttpResponseMessage(item.Status)
            {
                Content = new StringContent(item.Body, Encoding.UTF8, "application/json")
            }));
            using var client = new HttpClient(handler);
            var service = new SystemOneService(client, new SystemOneServiceConfiguration
            {
                BaseUrl = "https://provider.test",
                ApiKey = "secret-key",
                Model = "model",
                TimeoutSeconds = 5
            });

            var exception = await Assert.ThrowsAsync<SystemOneHttpException>(() => service.EvaluateAsync(input));

            Assert.Equal(item.Status, exception.StatusCode);
            Assert.Equal(item.HasDetails, exception.ProviderError is not null);
            Assert.DoesNotContain("privateStateValue", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("secret-key", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, handler.RequestCount);

            if (exception.ProviderError is not null)
            {
                var details = string.Join(" ", exception.ProviderError.Details);
                Assert.DoesNotContain("privateStateValue", details, StringComparison.Ordinal);
                Assert.DoesNotContain("secret-key", details, StringComparison.Ordinal);
                Assert.DoesNotContain("secretQuestionName", details, StringComparison.Ordinal);
                Assert.Contains("[redacted]", details, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public async Task MalformedSuccessAndTransportFailuresRemainExplicit()
    {
        var malformedHandler = new RecordingHandler((_, _) => Task.FromResult(JsonResponse("{not-json")));
        using var malformedClient = new HttpClient(malformedHandler);
        var malformedService = new SystemOneService(malformedClient, CreateConfiguration());
        await Assert.ThrowsAsync<SystemOneResponseException>(() => malformedService.EvaluateAsync(CreateInput(new SystemOneNoulQuestion())));
        Assert.Equal(1, malformedHandler.RequestCount);

        var transportHandler = new RecordingHandler((_, _) => throw new HttpRequestException("transport failure"));
        using var transportClient = new HttpClient(transportHandler);
        var transportService = new SystemOneService(transportClient, CreateConfiguration());
        await Assert.ThrowsAsync<HttpRequestException>(() => transportService.EvaluateAsync(CreateInput(new SystemOneNoulQuestion())));
        Assert.Equal(1, transportHandler.RequestCount);
    }

    [Fact]
    public async Task TimeoutAndCallerCancellationRemainDistinguishable()
    {
        var timeoutHandler = new RecordingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var timeoutClient = new HttpClient(timeoutHandler);
        var timeoutService = new SystemOneService(timeoutClient, new SystemOneServiceConfiguration
        {
            BaseUrl = "https://provider.test",
            ApiKey = "secret",
            Model = "model",
            TimeoutSeconds = 1
        });

        await Assert.ThrowsAsync<SystemOneTimeoutException>(() => timeoutService.EvaluateAsync(CreateInput(new SystemOneNoulQuestion())));
        Assert.Equal(1, timeoutHandler.RequestCount);

        var cancellationHandler = new RecordingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var cancellationClient = new HttpClient(cancellationHandler);
        var cancellationService = new SystemOneService(cancellationClient, CreateConfiguration());
        using var cancellation = new CancellationTokenSource();
        var evaluation = cancellationService.EvaluateAsync(CreateInput(new SystemOneNoulQuestion()), cancellation.Token);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => evaluation);
        Assert.Equal(1, cancellationHandler.RequestCount);
    }

    private static SystemOneServiceConfiguration CreateConfiguration() => new()
    {
        BaseUrl = "https://provider.test/",
        ApiKey = "secret",
        Model = "jev-1.14.0",
        TimeoutSeconds = 5
    };

    private static SystemOneInput CreateInput(
        SystemOneQuestion question,
        SystemOneJsonValue? state = null) =>
        new(state ?? SystemOneJsonValue.From("text"), new Dictionary<string, SystemOneQuestion> { ["question"] = question });

    private static SystemOneInput CreateMixedInput() =>
        new(SystemOneJsonValue.From("text"), new Dictionary<string, SystemOneQuestion>
        {
            ["noulQuestion"] = new SystemOneNoulQuestion(),
            ["choiceQuestion"] = new SystemOneChoiceQuestion
            {
                Criteria = new Dictionary<string, SystemOneJsonValue>
                {
                    ["yes"] = SystemOneJsonValue.From("yes"),
                    ["no"] = SystemOneJsonValue.From("no")
                }
            },
            ["scoreQuestion"] = new SystemOneScoreQuestion
            {
                Criteria =
                [
                    SystemOneJsonValue.From("low"),
                    SystemOneJsonValue.From("medium"),
                    SystemOneJsonValue.From("high")
                ]
            }
        });

    private static HttpResponseMessage JsonResponse(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    private static void AssertProtocolRequest(
        RecordedRequest request,
        string expectedUri,
        string expectedApiKey,
        string expectedModel)
    {
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(new Uri(expectedUri), request.Uri);
        Assert.Equal("Bearer", request.AuthorizationScheme);
        Assert.Equal(expectedApiKey, request.AuthorizationParameter);
        Assert.Equal("application/json", request.ContentType);
        Assert.False(request.HasImagesHeader);
        Assert.False(request.HasLongContextHeader);

        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal(expectedModel, body.RootElement.GetProperty("model").GetString());
        Assert.Equal(JsonValueKind.Object, body.RootElement.GetProperty("state").ValueKind);
        Assert.Equal(3, body.RootElement.GetProperty("questions").EnumerateObject().Count());
        Assert.Equal("noul", body.RootElement.GetProperty("questions").GetProperty("noulQuestion").GetProperty("type").GetString());
        Assert.Equal("choice", body.RootElement.GetProperty("questions").GetProperty("choiceQuestion").GetProperty("type").GetString());
        Assert.Equal("score", body.RootElement.GetProperty("questions").GetProperty("scoreQuestion").GetProperty("type").GetString());
        Assert.True(body.RootElement.GetProperty("questions").GetProperty("choiceQuestion")
            .GetProperty("criteria").TryGetProperty("userChoiceKey", out _));
        Assert.False(body.RootElement.TryGetProperty("assets", out _));
    }

    private static async Task<HttpResponseMessage> CreateSuccessResponseAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        var answers = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var questionProperty in body.RootElement.GetProperty("questions").EnumerateObject())
        {
            var name = questionProperty.Name;
            var question = questionProperty.Value;
            var type = question.GetProperty("type").GetString();
            if (type == "noul")
            {
                answers.Add(name, new { type = "noul", noul = 0.75 });
            }
            else if (type == "choice")
            {
                var keys = question.GetProperty("criteria").EnumerateObject().Select(property => property.Name).ToArray();
                answers.Add(name, new
                {
                    type = "choice",
                    choice = keys[0],
                    confidence = 0.8,
                    probabilities = keys.ToDictionary(key => key, _ => 1d / keys.Length, StringComparer.Ordinal)
                });
            }
            else
            {
                var descriptions = question.GetProperty("criteria").EnumerateArray().ToArray();
                var keys = Enumerable.Range(0, descriptions.Length).Select(index => index.ToString()).ToArray();
                answers.Add(name, new
                {
                    type = "score",
                    score = 0d,
                    confidence = 0.8,
                    legend = keys.Select((key, index) => (key, value: descriptions[index]))
                        .ToDictionary(item => item.key, item => item.value, StringComparer.Ordinal),
                    probabilities = keys.ToDictionary(key => key, _ => 1d / keys.Length, StringComparer.Ordinal)
                });
            }
        }

        var responseBody = JsonSerializer.Serialize(new
        {
            model = "jev-1.14.0",
            answers,
            usage = new { input_tokens = 20, output_tokens = 3 }
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
    }

    private sealed record UnsupportedQuestion : SystemOneQuestion;

    private const string ValidMixedResponse = """
        {
          "model":"jev-1.14.0",
          "answers":{
            "noulQuestion":{"type":"noul","noul":0.98},
            "choiceQuestion":{"type":"choice","choice":"yes","confidence":0.7,"probabilities":{"yes":0.7,"no":0.3}},
            "scoreQuestion":{"type":"score","score":1.7,"confidence":0.55,"legend":{"0":"low","1":{"label":"medium"},"2":["high"]},"probabilities":{"0":0.1,"1":0.2,"2":0.7}}
          },
          "usage":{"input_tokens":120,"output_tokens":12},
          "additive_property":true
        }
        """;

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri? Uri,
        string? AuthorizationScheme,
        string? AuthorizationParameter,
        string? ContentType,
        string Body,
        bool HasImagesHeader,
        bool HasLongContextHeader);

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> createResponse) : HttpMessageHandler
    {
        private readonly object _sync = new();
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        public List<string> RequestBodies { get; } = [];

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            lock (_sync)
            {
                RequestBodies.Add(body);
                Requests.Add(new RecordedRequest(
                    request.Method,
                    request.RequestUri,
                    request.Headers.Authorization?.Scheme,
                    request.Headers.Authorization?.Parameter,
                    request.Content?.Headers.ContentType?.MediaType,
                    body,
                    request.Headers.Contains("TypeSafe-Danger-Zone-Images"),
                    request.Headers.Contains("TypeSafe-Danger-Zone-Long-Context")));
            }

            return await createResponse(request, cancellationToken);
        }
    }
}
