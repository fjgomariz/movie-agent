using MovieAgent.Agent;
using MovieAgent.Models;
using MovieAgent.Observability;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using Azure.Identity;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var logLevel = builder.Configuration["LOG_LEVEL"];
if (!string.IsNullOrWhiteSpace(logLevel) && Enum.TryParse<LogLevel>(logLevel, true, out var level))
{
    builder.Logging.SetMinimumLevel(level);
}

// Add OpenTelemetry with Application Insights
builder.Services.AddMovieAgentOpenTelemetry(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks();

// Configure AI Chat Client for Foundry
var projectEndpoint = builder.Configuration["AZURE_AI_PROJECT_ENDPOINT"];
var modelDeploymentName = builder.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"] ?? "gpt-4o-mini";

if (string.IsNullOrWhiteSpace(projectEndpoint))
{
    throw new InvalidOperationException(
        "AZURE_AI_PROJECT_ENDPOINT environment variable is required. " +
        "Please set it to your Azure AI Foundry project endpoint.");
}

builder.Services.AddSingleton<IChatClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Initializing AI Chat Client with endpoint: {Endpoint}, model: {Model}",
        projectEndpoint, modelDeploymentName);

    // Create Azure OpenAI client with DefaultAzureCredential
    var credential = new DefaultAzureCredential();
    var openAIClient = new AzureOpenAIClient(
        new Uri(projectEndpoint),
        credential);

    // Get chat completion client and convert to IChatClient
    var chatCompletionClient = openAIClient.GetChatClient(modelDeploymentName);
    var chatClient = chatCompletionClient.AsIChatClient();

    // Wrap with OpenTelemetry support (built-in)
    return new OpenTelemetryChatClient(chatClient, sourceName: "MovieAgent.AI");
});

// Register the Movie Finder Agent
builder.Services.AddSingleton<MovieFinderAgent>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

// Health endpoints
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "Movie Finder Agent",
    version = "1.0.0",
    status = "running",
    endpoints = new[]
    {
        "POST /api/movie - Find movie by description",
        "GET /healthz - Health check",
        "GET /readyz - Readiness check"
    }
}));

// Movie search endpoint
app.MapPost("/api/movie", async (
    MovieRequest request,
    MovieFinderAgent agent,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    using var activity = Activity.Current;
    activity?.SetTag("movie.description", request.Description);
    activity?.SetTag("movie.conversationId", request.ConversationId);

    logger.LogInformation("Received movie search request");

    if (string.IsNullOrWhiteSpace(request.Description))
    {
        logger.LogWarning("Invalid request: description is required");
        return Results.BadRequest(new { error = "Description is required" });
    }

    try
    {
        var response = await agent.FindMovieAsync(request, cancellationToken);
        
        activity?.SetTag("movie.result.title", response.Result.Title);
        activity?.SetTag("movie.result.confidence", response.Result.Confidence);
        
        logger.LogInformation("Successfully processed movie search request");
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing movie search request");
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        return Results.Problem(
            title: "Error processing request",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("FindMovie");

app.Run();
