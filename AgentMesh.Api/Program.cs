using System.Reflection;
using System.Text.Json.Serialization;
using AgentMesh.Application;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Authentication;
using AgentMesh.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
namespace AgentMesh.Api;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        AgentMeshRuntime.ConfigureConfiguration(builder.Configuration, builder.Environment.EnvironmentName);
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        AgentMeshRuntime.RegisterCommonServices(builder.Services, builder.Configuration);

        var apiKeyConfiguration = builder.Configuration
            .GetSection(ApiKeyAuthenticationConfiguration.SectionName)
            .Get<ApiKeyAuthenticationConfiguration>() ?? new ApiKeyAuthenticationConfiguration();

        if (string.IsNullOrWhiteSpace(apiKeyConfiguration.ApiKey))
        {
            throw new InvalidOperationException($"Missing API key configuration: '{ApiKeyAuthenticationConfiguration.SectionName}:ApiKey'.");
        }

        builder.Services
            .AddOptions<ApiKeyAuthenticationConfiguration>()
            .Bind(builder.Configuration.GetSection(ApiKeyAuthenticationConfiguration.SectionName))
            .Services
            .AddSingleton(sp => sp.GetRequiredService<IOptions<ApiKeyAuthenticationConfiguration>>().Value);

        builder.Services.AddScoped<CallbackNotifierContext>();
        builder.Services.AddScoped<IWorkflowProgressNotifier, CallbackWorkflowProgressNotifier>();
        builder.Services.AddHttpClient(nameof(CallbackWorkflowProgressNotifier));
        builder.Services.AddAuthentication(ApiKeyAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AgentMesh API",
                Version = "v1",
                Description = "AgentMesh AI Agent Orchestration and Pipeline Execution API."
            });

            options.AddSecurityDefinition(ApiKeyAuthenticationDefaults.SchemeName, new OpenApiSecurityScheme
            {
                Name = apiKeyConfiguration.HeaderName,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Provide the API key to access protected endpoints."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = ApiKeyAuthenticationDefaults.SchemeName
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFiles = new[]
            {
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
                "AgentMesh.xml",
                "AgentMesh.Application.xml"
            };

            foreach (var xmlFile in xmlFiles)
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            }
        });

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseSwagger();
        app.UseSwaggerUI();

        await app.RunAsync();
    }
}