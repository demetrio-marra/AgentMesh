using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Contracts.Models.SystemOne;
using AgentMesh.Contracts.Services;
using AgentMesh.Infrastructure.SystemOne.Models;

namespace AgentMesh.Infrastructure.SystemOne;

public sealed class SystemOneService : ISystemOneService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly SystemOneServiceConfiguration _configuration;
    private readonly Uri _endpoint;

    public SystemOneService(HttpClient httpClient, SystemOneServiceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(configuration);

        _endpoint = configuration.ValidateAndGetEndpoint();
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);
    }

    public async Task<SystemOneResult> EvaluateAsync(SystemOneInput input, CancellationToken cancellationToken = default)
    {
        var requestDto = CreateRequest(input);
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = JsonContent.Create(requestDto, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SystemOneTimeoutException("The SystemOne request timed out.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var providerError = response.StatusCode is System.Net.HttpStatusCode.BadRequest
                    or System.Net.HttpStatusCode.UnprocessableEntity
                    ? await ReadProviderErrorAsync(response, input, cancellationToken)
                    : null;
                throw new SystemOneHttpException(response.StatusCode, providerError);
            }

            string responseBody;
            try
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                throw new SystemOneTimeoutException("Reading the SystemOne response timed out.", exception);
            }

            SystemOneResponseDto responseDto;
            try
            {
                responseDto = JsonSerializer.Deserialize<SystemOneResponseDto>(responseBody, JsonOptions)
                    ?? throw new JsonException();
            }
            catch (JsonException exception)
            {
                throw new SystemOneResponseException("The SystemOne provider returned malformed JSON or an incomplete response.", exception);
            }
            catch (NotSupportedException exception)
            {
                throw new SystemOneResponseException("The SystemOne provider returned an unsupported response discriminator.", exception);
            }

            return MapResponse(responseDto, input);
        }
    }

    private SystemOneRequestDto CreateRequest(SystemOneInput input)
    {
        if (input is null)
        {
            throw new SystemOneValidationException("A SystemOne input is required.");
        }

        ValidateJsonValue(input.State.Value, allowNull: false, "state");
        if (input.Questions.Count == 0)
        {
            throw new SystemOneValidationException("At least one named question is required.");
        }

        var questions = new Dictionary<string, SystemOneQuestionDto>(input.Questions.Count, StringComparer.Ordinal);
        foreach (var (name, question) in input.Questions)
        {
            if (string.IsNullOrWhiteSpace(name) || question is null)
            {
                throw new SystemOneValidationException("Every question must have a name and a supported question type.");
            }

            questions.Add(name, MapQuestion(question));
        }

        return new SystemOneRequestDto
        {
            Model = _configuration.Model,
            State = input.State.Value,
            Questions = questions
        };
    }

    private static SystemOneQuestionDto MapQuestion(SystemOneQuestion question) =>
        question switch
        {
            SystemOneNoulQuestion noul => MapNoulQuestion(noul),
            SystemOneChoiceQuestion choice => MapChoiceQuestion(choice),
            SystemOneScoreQuestion score => MapScoreQuestion(score),
            _ => throw new SystemOneValidationException("The input contains an unsupported question type.")
        };

    private static SystemOneNoulQuestionDto MapNoulQuestion(SystemOneNoulQuestion question)
    {
        ValidateOptionalJsonValue(question.Instructions, "instructions");
        if (question.Criteria is null)
        {
            throw new SystemOneValidationException("Noul criteria cannot be null.");
        }

        var criteria = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (value, description) in question.Criteria)
        {
            if (description is null)
            {
                throw new SystemOneValidationException("Noul criteria descriptions must be explicit JSON values.");
            }

            ValidateJsonValue(description.Value, allowNull: true, "criteria");
            criteria.Add(value ? "true" : "false", description.Value);
        }

        return new SystemOneNoulQuestionDto
        {
            Instructions = question.Instructions?.Value,
            Criteria = criteria.Count == 0 ? null : criteria
        };
    }

    private static SystemOneChoiceQuestionDto MapChoiceQuestion(SystemOneChoiceQuestion question)
    {
        ValidateOptionalJsonValue(question.Instructions, "instructions");
        if (question.Criteria is null)
        {
            throw new SystemOneValidationException("Choice criteria cannot be null.");
        }

        if (question.Criteria.Count is < 1 or > 255)
        {
            throw new SystemOneValidationException("Choice criteria must contain between 1 and 255 entries.");
        }

        var criteria = new Dictionary<string, JsonElement>(question.Criteria.Count, StringComparer.Ordinal);
        foreach (var (key, description) in question.Criteria)
        {
            if (description is null)
            {
                throw new SystemOneValidationException("Choice criteria descriptions must be explicit JSON values.");
            }

            ValidateJsonValue(description.Value, allowNull: true, "criteria");
            criteria.Add(key, description.Value);
        }

        return new SystemOneChoiceQuestionDto
        {
            Instructions = question.Instructions?.Value,
            Criteria = criteria
        };
    }

    private static SystemOneScoreQuestionDto MapScoreQuestion(SystemOneScoreQuestion question)
    {
        ValidateOptionalJsonValue(question.Instructions, "instructions");
        if (question.Criteria is null)
        {
            throw new SystemOneValidationException("Score criteria cannot be null.");
        }

        if (question.Criteria.Count is < 1 or > 10)
        {
            throw new SystemOneValidationException("Score criteria must contain between 1 and 10 ordered levels.");
        }

        var criteria = new List<JsonElement>(question.Criteria.Count);
        foreach (var description in question.Criteria)
        {
            if (description is null)
            {
                throw new SystemOneValidationException("Score criteria descriptions must be JSON values.");
            }

            ValidateJsonValue(description.Value, allowNull: false, "criteria");
            criteria.Add(description.Value);
        }

        return new SystemOneScoreQuestionDto
        {
            Instructions = question.Instructions?.Value,
            Criteria = criteria
        };
    }

    private static void ValidateOptionalJsonValue(SystemOneJsonValue? value, string location)
    {
        if (value is not null)
        {
            ValidateJsonValue(value.Value, allowNull: true, location);
        }
    }

    private static void ValidateJsonValue(JsonElement value, bool allowNull, string location)
    {
        var valid = value.ValueKind switch
        {
            JsonValueKind.String or JsonValueKind.Object or JsonValueKind.Array => true,
            JsonValueKind.Null => allowNull,
            _ => false
        };

        if (!valid)
        {
            throw new SystemOneValidationException($"SystemOne {location} must be a string, object, array, or an allowed null value.");
        }
    }

    private static SystemOneResult MapResponse(SystemOneResponseDto response, SystemOneInput input)
    {
        if (string.IsNullOrWhiteSpace(response.Model)
            || response.Answers is null
            || response.Usage is null
            || response.Usage.InputTokens < 0
            || response.Usage.OutputTokens < 0)
        {
            throw new SystemOneResponseException("The SystemOne provider returned a response with missing or invalid required fields.");
        }

        if (response.Answers.Count != input.Questions.Count
            || input.Questions.Keys.Any(name => !response.Answers.ContainsKey(name)))
        {
            throw new SystemOneResponseException("The SystemOne response answer names do not match the submitted questions.");
        }

        var answers = new Dictionary<string, SystemOneAnswer>(response.Answers.Count, StringComparer.Ordinal);
        foreach (var (name, answerDto) in response.Answers)
        {
            if (answerDto is null || !input.Questions.TryGetValue(name, out var question))
            {
                throw new SystemOneResponseException("The SystemOne response contains an unexpected answer.");
            }

            answers.Add(name, MapAnswer(answerDto, question));
        }

        return new SystemOneResult(
            response.Model,
            new ReadOnlyDictionary<string, SystemOneAnswer>(answers),
            new SystemOneUsage(response.Usage.InputTokens, response.Usage.OutputTokens));
    }

    private static SystemOneAnswer MapAnswer(SystemOneAnswerDto answer, SystemOneQuestion question) =>
        (answer, question) switch
        {
            (SystemOneNoulAnswerDto noul, SystemOneNoulQuestion) => MapNoulAnswer(noul),
            (SystemOneChoiceAnswerDto choice, SystemOneChoiceQuestion choiceQuestion) => MapChoiceAnswer(choice, choiceQuestion),
            (SystemOneScoreAnswerDto score, SystemOneScoreQuestion scoreQuestion) => MapScoreAnswer(score, scoreQuestion),
            _ => throw new SystemOneResponseException("The SystemOne answer type does not match its submitted question.")
        };

    private static SystemOneNoulAnswer MapNoulAnswer(SystemOneNoulAnswerDto answer)
    {
        ValidateProbability(answer.Noul, "Noul probability");
        return new SystemOneNoulAnswer(answer.Noul);
    }

    private static SystemOneChoiceAnswer MapChoiceAnswer(SystemOneChoiceAnswerDto answer, SystemOneChoiceQuestion question)
    {
        ValidateProbability(answer.Confidence, "Choice confidence");
        if (string.IsNullOrEmpty(answer.Choice)
            || answer.Probabilities is null
            || !question.Criteria.ContainsKey(answer.Choice)
            || !SameKeys(answer.Probabilities.Keys, question.Criteria.Keys))
        {
            throw new SystemOneResponseException("The SystemOne Choice answer does not match its submitted criteria.");
        }

        foreach (var probability in answer.Probabilities.Values)
        {
            ValidateProbability(probability, "Choice probability");
        }

        return new SystemOneChoiceAnswer(
            answer.Choice,
            answer.Confidence,
            new ReadOnlyDictionary<string, double>(
                new Dictionary<string, double>(answer.Probabilities, StringComparer.Ordinal)));
    }

    private static SystemOneScoreAnswer MapScoreAnswer(SystemOneScoreAnswerDto answer, SystemOneScoreQuestion question)
    {
        ValidateProbability(answer.Confidence, "Score confidence");
        if (answer.Legend is null
            || answer.Probabilities is null
            || !double.IsFinite(answer.Score)
            || answer.Score < 0
            || answer.Score > question.Criteria.Count - 1)
        {
            throw new SystemOneResponseException("The SystemOne Score answer contains an invalid score or missing fields.");
        }

        var expectedKeys = Enumerable.Range(0, question.Criteria.Count).Select(index => index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (!SameKeys(answer.Legend.Keys, expectedKeys) || !SameKeys(answer.Probabilities.Keys, expectedKeys))
        {
            throw new SystemOneResponseException("The SystemOne Score legend and probabilities do not match its submitted criteria.");
        }

        foreach (var (key, legendValue) in answer.Legend)
        {
            if (legendValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                || !IsDescriptionShape(legendValue.ValueKind))
            {
                throw new SystemOneResponseException("The SystemOne Score legend contains an invalid value.");
            }

            if (!answer.Probabilities.TryGetValue(key, out var probability))
            {
                throw new SystemOneResponseException("The SystemOne Score probabilities do not match its legend.");
            }

            ValidateProbability(probability, "Score probability");
        }

        var legend = answer.Legend.ToDictionary(
            pair => pair.Key,
            pair => new SystemOneJsonValue(pair.Value),
            StringComparer.Ordinal);
        return new SystemOneScoreAnswer(
            answer.Score,
            answer.Confidence,
            new ReadOnlyDictionary<string, SystemOneJsonValue>(legend),
            new ReadOnlyDictionary<string, double>(
                new Dictionary<string, double>(answer.Probabilities, StringComparer.Ordinal)));
    }

    private static bool IsDescriptionShape(JsonValueKind valueKind) =>
        valueKind is JsonValueKind.String or JsonValueKind.Object or JsonValueKind.Array;

    private static bool SameKeys(IEnumerable<string> actual, IEnumerable<string> expected)
    {
        var actualSet = new HashSet<string>(actual, StringComparer.Ordinal);
        var expectedSet = new HashSet<string>(expected, StringComparer.Ordinal);
        return actualSet.SetEquals(expectedSet);
    }

    private static void ValidateProbability(double value, string field)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
        {
            throw new SystemOneResponseException($"The SystemOne response contains an invalid {field}.");
        }
    }

    private async Task<SystemOneProviderError?> ReadProviderErrorAsync(
        HttpResponseMessage response,
        SystemOneInput input,
        CancellationToken cancellationToken)
    {
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SystemOneTimeoutException("Reading the SystemOne error response timed out.", exception);
        }

        try
        {
            var errorDto = JsonSerializer.Deserialize<SystemOneProviderErrorDto>(body, JsonOptions);
            if (errorDto?.Detail is not { } detail)
            {
                return null;
            }

            var details = new List<string>();
            CollectErrorDetails(detail, details);
            if (details.Count == 0)
            {
                return null;
            }

            var sensitiveValues = CollectSensitiveValues(input);
            for (var index = 0; index < details.Count; index++)
            {
                details[index] = SanitizeErrorDetail(details[index], sensitiveValues);
            }

            return new SystemOneProviderError(details);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void CollectErrorDetails(JsonElement value, List<string> details)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                details.Add(value.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    CollectErrorDetails(item, details);
                }

                break;
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                {
                    if (property.Name is "msg" or "message" or "detail" or "type")
                    {
                        CollectErrorDetails(property.Value, details);
                    }
                    else if (property.Name == "loc" && property.Value.ValueKind == JsonValueKind.Array)
                    {
                        details.Add($"Location: {string.Join(".", property.Value.EnumerateArray().Select(FormatLocationPart))}");
                    }
                }

                break;
        }
    }

    private static string FormatLocationPart(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.ValueKind == JsonValueKind.Number
                ? value.GetRawText()
                : string.Empty;

    private HashSet<string> CollectSensitiveValues(SystemOneInput input)
    {
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { _configuration.ApiKey };
        AddJsonSensitiveValues(input.State.Value, values);
        values.Add(input.State.Value.GetRawText());

        foreach (var (name, question) in input.Questions)
        {
            values.Add(name);
            switch (question)
            {
                case SystemOneNoulQuestion noul:
                    AddJsonSensitiveValues(noul.Instructions?.Value, values);
                    foreach (var (key, value) in noul.Criteria)
                    {
                        values.Add(key ? "true" : "false");
                        AddJsonSensitiveValues(value.Value, values);
                    }

                    break;
                case SystemOneChoiceQuestion choice:
                    AddJsonSensitiveValues(choice.Instructions?.Value, values);
                    foreach (var (key, value) in choice.Criteria)
                    {
                        values.Add(key);
                        AddJsonSensitiveValues(value.Value, values);
                    }

                    break;
                case SystemOneScoreQuestion score:
                    AddJsonSensitiveValues(score.Instructions?.Value, values);
                    foreach (var value in score.Criteria)
                    {
                        AddJsonSensitiveValues(value.Value, values);
                    }

                    break;
            }
        }

        values.Remove(string.Empty);
        return values;
    }

    private static void AddJsonSensitiveValues(JsonElement? element, HashSet<string> values)
    {
        if (element is null)
        {
            return;
        }

        var value = element.Value;
        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                values.Add(text);
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                values.Add(property.Name);
                AddJsonSensitiveValues(property.Value, values);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                AddJsonSensitiveValues(item, values);
            }
        }
    }

    private static string SanitizeErrorDetail(string detail, HashSet<string> sensitiveValues)
    {
        foreach (var value in sensitiveValues.OrderByDescending(value => value.Length))
        {
            detail = detail.Replace(value, "[redacted]", StringComparison.OrdinalIgnoreCase);
        }

        const int maxLength = 512;
        return detail.Length <= maxLength ? detail : detail[..maxLength];
    }
}
