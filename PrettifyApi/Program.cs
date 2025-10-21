using PrettifyApi.Models;
using PrettifyApi.Services;

var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// Add configuration from appsettings.json
/// </summary>
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

/// <summary>
/// Configure logging
/// </summary>
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

/// <summary>
/// Retrieve Gemini API key from configuration
/// </summary>
string? apiKey = builder.Configuration["Gemini:ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Error: Gemini API key is missing or invalid in appsettings.json.");
    return;
}

/// <summary>
/// Register GeminiService as a singleton using dependency injection
/// </summary>
builder.Services.AddSingleton(new GeminiService(apiKey, "PrettifyPrompt.txt"));

/// <summary>
/// Configure Swagger and CORS
/// </summary>
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/// <summary>
/// Configure CORS policy for React app
/// </summary>
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // React dev server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Optional, only if you use cookies or auth
    });
});

var app = builder.Build();

/// <summary>
/// Use Swagger UI in development
/// </summary>
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prettifier API v1");
        c.RoutePrefix = string.Empty;
    });
}

/// <summary>
/// Apply CORS before any endpoints
/// </summary>
app.UseCors("AllowReactApp");

/// <summary>
/// Use HTTPS Redirection (optional — disable if causing redirects)
/// </summary>
// app.UseHttpsRedirection();

/// <summary>
/// Health check endpoint
/// </summary>
app.MapGet("/", () => "Prettifier API is running!");

/// <summary>
/// Unified Prettify Endpoint (text or code)
/// Accepts a JSON body: { "text": "your input" }
/// Returns prettified JSON response
/// </summary>
app.MapPost("/prettify", async (PrettifyRequest request, GeminiService geminiService, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request?.Text))
        return Results.BadRequest(new { error = "Input cannot be empty." });

    try
    {
        var jsonResult = await geminiService.PrettifyAsync(request.Text);
        return Results.Content(jsonResult, "application/json");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error calling Gemini API");
        return Results.Problem(detail: ex.Message, title: "Gemini API Error");
    }
})
.WithName("Prettify")
.WithTags("Prettifier");

/// <summary>
/// Streaming Prettify Endpoint using Server-Sent Events (SSE)
/// Accepts a JSON body: { "text": "your input" }
/// Streams partial prettified JSON as it is generated
/// </summary>
app.MapPost("/prettify/stream", async (PrettifyRequest request, GeminiService geminiService, HttpResponse response, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request?.Text))
    {
        response.StatusCode = 400;
        await response.WriteAsJsonAsync(new { error = "Input cannot be empty." });
        return;
    }

    // Use "text/event-stream" for streaming
    response.ContentType = "text/event-stream";

    try
    {
        await foreach (var chunk in geminiService.PrettifyStreamAsync(request.Text))
        {
            await response.WriteAsync(chunk);
            await response.Body.FlushAsync();
        }
    }
    catch (OperationCanceledException)
    {
        logger.LogWarning("Streaming was canceled.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during streaming.");
    }
})
.WithName("PrettifyStream")
.WithTags("Prettifier");

/// <summary>
/// Run the application
/// </summary>
app.Run();
