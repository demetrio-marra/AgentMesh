using AgentMeshWeb.Configuration;
using AgentMeshWeb.Hubs;
using AgentMeshWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSignalR();
builder.Services.AddOptions<ApiConfiguration>()
    .Bind(builder.Configuration.GetSection(ApiConfiguration.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ConversationSummarizationConfiguration>()
    .Bind(builder.Configuration.GetSection(ConversationSummarizationConfiguration.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ChatContextConfiguration>()
    .Bind(builder.Configuration.GetSection(ChatContextConfiguration.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient<IAgentMeshApiClient, AgentMeshApiClient>((services, client) =>
{
    var configuration = services.GetRequiredService<IOptions<ApiConfiguration>>().Value;
    client.BaseAddress = new Uri(configuration.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = Timeout.InfiniteTimeSpan;
    client.DefaultRequestHeaders.Add(configuration.HeaderName, configuration.ApiKey);
});
builder.Services.AddSingleton<IChatContextStore, InMemoryChatContextStore>();
builder.Services.AddSingleton<ChatOperationRegistry>();
builder.Services.AddSingleton<ChatCoordinator>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'";
    await next();
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}");
app.MapHub<ChatHub>("/hubs/chat");

app.Run();