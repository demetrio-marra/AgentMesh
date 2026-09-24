var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
        .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()?.Addresses;

    if (addresses is not null)
    {
        foreach (var address in addresses)
        {
            Console.WriteLine($"Listening for callbacks on: {address}/callback");
        }
    }
});

app.MapGet("/", () => "CallbackViewer is running.");

app.MapPost("/callback/{**path}", async (HttpContext context, string? path) =>
{
    var request = context.Request;

    string body;
    using (var reader = new StreamReader(request.Body))
    {
        body = await reader.ReadToEndAsync();
    }

    var headers = string.Join(Environment.NewLine, request.Headers.Select(h => $"  {h.Key}: {h.Value}"));

    Console.WriteLine("=== Callback received ===");
    Console.WriteLine($"Time: {DateTimeOffset.UtcNow:O}");
    Console.WriteLine($"Method: {request.Method}");
    Console.WriteLine($"Path: /{path}");
    Console.WriteLine($"Query: {request.QueryString}");
    Console.WriteLine("Headers:");
    Console.WriteLine(headers);
    Console.WriteLine("Body:");
    Console.WriteLine(string.IsNullOrEmpty(body) ? "(empty)" : body);
    Console.WriteLine("==========================");

    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsync("OK");
});

app.Run();
