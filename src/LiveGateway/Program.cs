using System.Net.Http.Headers;
using LiveGateway;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<LiveOptions>()
    .BindConfiguration(LiveOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient("openai-live", (services, client) =>
{
    var apiKey = services.GetRequiredService<IConfiguration>()["OPENAI_API_KEY"]
        ?? throw new InvalidOperationException("OPENAI_API_KEY is missing.");

    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", apiKey);
});

var app = builder.Build();

app.MapPost("/live/session", async (
    HttpRequest request,
    IHttpClientFactory clients,
    IOptions<LiveOptions> options,
    CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(request.Body);
    var sdpOffer = await reader.ReadToEndAsync(cancellationToken);

    if (sdpOffer.Length is < 10 or > 1_000_000)
        return Results.BadRequest("Invalid SDP offer.");

    var o = options.Value;
    var payload = new
    {
        session = new
        {
            model = o.Model,
            instructions = o.Instructions,
            wake_policy = new { type = "speech", vad_threshold = o.VadThreshold },
            idle_timeout_s = o.IdleTimeoutSeconds
        },
        transport = new { type = "webrtc", sdp = sdpOffer }
    };

    using var response = await clients
        .CreateClient("openai-live")
        .PostAsJsonAsync("live/sessions", payload, cancellationToken);

    var json = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(json, "application/json",
        statusCode: (int)response.StatusCode);
});

app.Run();

public partial class Program;
