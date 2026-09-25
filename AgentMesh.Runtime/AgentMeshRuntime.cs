using System.Reflection;
using System.Text.Json.Serialization;
using AgentMesh.Application;
using AgentMesh.Authentication;
using AgentMesh.Configuration;
using AgentMesh.Runtime.Services;
using AgentMesh.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AgentMesh.Runtime;

public static class AgentMeshRuntime
{
    public static void LoadAgentMesh<TPlugin>(WebApplicationBuilder builder)
        where TPlugin : IAgentMeshPlugin
    {
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
        ApplicationRuntime.RegisterCommonServices(builder.Services, builder.Configuration);
        PluginComponentRegistry.Register(builder.Services, typeof(TPlugin).Assembly);

        var apiKeyConfiguration = builder.Configuration
            .GetSection(ApiKeyAuthenticationConfiguration.SectionName)
            .Get<ApiKeyAuthenticationConfiguration>() ?? new ApiKeyAuthenticationConfiguration();
        if (string.IsNullOrWhiteSpace(apiKeyConfiguration.ApiKey))
        {
            throw new InvalidOperationException($"Missing API key configuration: '{ApiKeyAuthenticationConfiguration.SectionName}:ApiKey'.");
        }

        builder.Services.AddOptions<ApiKeyAuthenticationConfiguration>()
            .Bind(builder.Configuration.GetSection(ApiKeyAuthenticationConfiguration.SectionName))
            .Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ApiKeyAuthenticationConfiguration>>().Value);
        builder.Services.AddScoped<CallbackNotifierContext>();
        builder.Services.AddScoped<IWorkflowProgressNotifier, CallbackWorkflowProgressNotifier>();
        builder.Services.AddHttpClient(nameof(CallbackWorkflowProgressNotifier));
        builder.Services.AddAuthentication(ApiKeyAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AgentMeshRuntime).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => ConfigureSwagger(options, apiKeyConfiguration));
    }

    public static Task StartAgentMesh(WebApplication app, CancellationToken cancellationToken = default)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseSwagger();
        app.UseSwaggerUI();
        return app.RunAsync(cancellationToken);
    }

    private static void ConfigureSwagger(SwaggerGenOptions options, ApiKeyAuthenticationConfiguration apiKeyConfiguration)
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "AgentMesh API", Version = "v1", Description = "AgentMesh AI Agent Orchestration and Pipeline Execution API." });
        options.AddSecurityDefinition(ApiKeyAuthenticationDefaults.SchemeName, new OpenApiSecurityScheme { Name = apiKeyConfiguration.HeaderName, Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Description = "Provide the API key to access protected endpoints." });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ApiKeyAuthenticationDefaults.SchemeName } }, Array.Empty<string>() } });
        foreach (var xmlFile in new[] { $"{Assembly.GetExecutingAssembly().GetName().Name}.xml", "AgentMesh.xml", "AgentMesh.Application.xml" })
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
}