using Xunit;

namespace CallbackViewer.Tests;

public class CallbackRequestFormatterTests
{
    [Fact]
    public void FormatIncludesRequestMetadataHeadersAndPayload()
    {
        var receivedAt = new DateTimeOffset(2026, 10, 13, 12, 30, 0, TimeSpan.Zero);

        var formatted = CallbackRequestFormatter.Format(
            receivedAt,
            "POST",
            "workflow/step",
            "?attempt=2",
            [new("Content-Type", "application/json"), new("X-Request-Id", "request-1")],
            "{\"step\":\"done\"}");

        Assert.Contains("Time: 2026-10-13T12:30:00.0000000+00:00", formatted);
        Assert.Contains("Method: POST", formatted);
        Assert.Contains("Path: /workflow/step", formatted);
        Assert.Contains("Query: ?attempt=2", formatted);
        Assert.Contains("  Content-Type: application/json", formatted);
        Assert.Contains("  X-Request-Id: request-1", formatted);
        Assert.Contains("{\"step\":\"done\"}", formatted);
    }

    [Fact]
    public void FormatMarksEmptyBodyAndPreservesEmptyHeaderSection()
    {
        var formatted = CallbackRequestFormatter.Format(
            DateTimeOffset.UnixEpoch,
            "POST",
            null,
            string.Empty,
            [],
            string.Empty);

        Assert.Contains("Path: /", formatted);
        Assert.Contains($"Headers:{Environment.NewLine}{Environment.NewLine}Body:", formatted);
        Assert.Contains("(empty)", formatted);
    }
}