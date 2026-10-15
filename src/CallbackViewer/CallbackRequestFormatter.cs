namespace CallbackViewer;

internal static class CallbackRequestFormatter
{
    public static string Format(
        DateTimeOffset receivedAt,
        string method,
        string? path,
        string query,
        IEnumerable<KeyValuePair<string, string>> headers,
        string? body)
    {
        var formattedHeaders = string.Join(Environment.NewLine, headers.Select(header => $"  {header.Key}: {header.Value}"));

        return string.Join(Environment.NewLine,
            "=== Callback received ===",
            $"Time: {receivedAt:O}",
            $"Method: {method}",
            $"Path: /{path}",
            $"Query: {query}",
            "Headers:",
            formattedHeaders,
            "Body:",
            string.IsNullOrEmpty(body) ? "(empty)" : body,
            "==========================");
    }
}