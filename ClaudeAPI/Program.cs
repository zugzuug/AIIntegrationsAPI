using AIIntegrationsAPI.Abstractions;
using AIIntegrationsAPI.Infrastructure;
using AIIntegrationsAPI.Models;
using AIIntegrationsAPI.Options;
using AIIntegrationsAPI.Providers;
using AIIntegrationsAPI.Security;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using System;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// Application Insights (safe to call even without a key)
// ----------------------------------------------------
builder.Services.AddApplicationInsightsTelemetry();

// ----------------------------------------------------
// Caching + chat session store
// ----------------------------------------------------
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IChatSessionStore, InMemoryChatSessionStore>();

// ----------------------------------------------------
// Serilog: console everywhere; files in Development;
//           App Insights if APPLICATIONINSIGHTS_CONNECTION_STRING is set
// ----------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .WriteTo.Console();

    // TODO: Move this to appsettings.Development.json
    if (ctx.HostingEnvironment.IsDevelopment())
    {
        cfg.WriteTo.File(
            path: "Logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            shared: true);
    }

    var aiConn = ctx.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    if (!string.IsNullOrWhiteSpace(aiConn))
    {
        var tc = services.GetService<TelemetryConfiguration>() ?? TelemetryConfiguration.CreateDefault();
        if (string.IsNullOrWhiteSpace(tc.ConnectionString))
            tc.ConnectionString = aiConn;

        // v4 sink API: use a converter instance
        cfg.WriteTo.ApplicationInsights(tc, new TraceTelemetryConverter());
    }
});

// ----------------------------------------------------
// Options & Providers
// ----------------------------------------------------
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai"));
builder.Services.AddHttpClient("Claude");
builder.Services.AddHttpClient("OpenAI");
builder.Services.AddSingleton<IChatProviderFactory, ChatProviderFactory>();

// API key options (owner + guests)
builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection("ApiKeys"));

// ----------------------------------------------------
// CORS (reads from Cors:Origins in appsettings if present)
// ----------------------------------------------------
var allowed = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowed).AllowAnyHeader().AllowAnyMethod());
});

// ----------------------------------------------------
// MVC + Swagger (with API-key padlock)
// ----------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AI Integrations API", Version = "v1" });
    
    // Provide a default example for ChatRequest
    c.MapType<ChatRequest>(() => new OpenApiSchema
    {
        Type = "object",
        Example = new OpenApiObject
        {
            ["messages"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["role"] = new OpenApiString("user"),
                    ["content"] = new OpenApiString("Hello Claude, can you tell me a joke?")
                }
            },
            ["provider"] = new OpenApiString("Claude") // or "OpenAI"]
        }
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Enter your API key (owner or guest).",
        Name = "x-api-key",                 // MUST match ApiKeyOptions.HeaderName (default)
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

// ----------------------------------------------------
// Rate limiting (simple global guard)
// ----------------------------------------------------
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("global", o =>
    {
        o.Window = TimeSpan.FromSeconds(1);
        o.PermitLimit = 5;
        o.QueueLimit = 0;
    });
});

var app = builder.Build();

// ----------------------------------------------------
// Middleware pipeline
// ----------------------------------------------------
app.UseSerilogRequestLogging();                 // request/response + timing

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "AI Integrations API Swagger";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Integrations API v1");
    });
}

// Global error handler -> RFC7807 ProblemDetails
app.UseExceptionHandler("/error");

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRateLimiter();

// API key middleware (bypasses swagger/health/OPTIONS in its own logic)
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Health & error endpoints
app.MapGet("/healthz", () => Results.Ok(new { ok = true }));

app.Map("/error", (HttpContext http) =>
    Results.Problem(
        title: "An unexpected error occurred.",
        statusCode: StatusCodes.Status500InternalServerError,
        instance: http.TraceIdentifier));

app.Run();


