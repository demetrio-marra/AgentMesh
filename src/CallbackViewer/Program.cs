using CallbackViewer;

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

    var formattedRequest = CallbackRequestFormatter.Format(
        DateTimeOffset.UtcNow,
        request.Method,
        path,
        request.QueryString.ToString(),
        request.Headers.Select(header => new KeyValuePair<string, string>(header.Key, header.Value.ToString())),
        body);
    Console.WriteLine(formattedRequest);

    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsync("OK");
});

app.Run();
