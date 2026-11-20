using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LiveGateway.Tests;

public class SessionEndpointTests
{
    private const string Offer = "v=0\r\no=- 0 0 IN IP4 127.0.0.1\r\ns=-\r\n";

    private sealed class FakeOpenAI(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }

    private static HttpClient CreateClient(FakeOpenAI fake, params (string Key, string Value)[] settings)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("OPENAI_API_KEY", "test-key");
            foreach (var (key, value) in settings) host.UseSetting(key, value);
            host.ConfigureServices(services =>
                services.AddHttpClient("openai-live")
                    .ConfigurePrimaryHttpMessageHandler(() => fake));
        });
        return factory.CreateClient();
    }

    [Fact]
    public async Task Sends_standby_policy_to_live_sessions()
    {
        var fake = new FakeOpenAI(HttpStatusCode.Created, "{\"session\":{\"id\":\"sess_1\"}}");
        var client = CreateClient(fake);

        var response = await client.PostAsync("/live/session", new StringContent(Offer));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(HttpMethod.Post, fake.Request!.Method);
        Assert.Equal("https://api.openai.com/v1/live/sessions", fake.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", fake.Request.Headers.Authorization!.Scheme);
        Assert.Equal("test-key", fake.Request.Headers.Authorization.Parameter);

        using var json = JsonDocument.Parse(fake.RequestBody!);
        var session = json.RootElement.GetProperty("session");
        Assert.Equal("gpt-live-1.1", session.GetProperty("model").GetString());
        Assert.Equal("speech", session.GetProperty("wake_policy").GetProperty("type").GetString());
        Assert.Equal(0.55, session.GetProperty("wake_policy").GetProperty("vad_threshold").GetDouble());
        Assert.Equal(10, session.GetProperty("idle_timeout_s").GetInt32());

        var transport = json.RootElement.GetProperty("transport");
        Assert.Equal("webrtc", transport.GetProperty("type").GetString());
        Assert.Equal(Offer, transport.GetProperty("sdp").GetString());
    }

    [Fact]
    public async Task Passes_openai_errors_through()
    {
        var fake = new FakeOpenAI(HttpStatusCode.BadRequest, "{\"error\":{\"message\":\"bad\"}}");
        var client = CreateClient(fake);

        var response = await client.PostAsync("/live/session", new StringContent(Offer));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("bad", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Rejects_empty_offer_without_calling_openai()
    {
        var fake = new FakeOpenAI(HttpStatusCode.Created, "{}");
        var client = CreateClient(fake);

        var response = await client.PostAsync("/live/session", new StringContent(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(fake.Request);
    }

    [Fact]
    public async Task Refuses_to_start_with_an_idle_timeout_outside_five_to_sixty()
    {
        var fake = new FakeOpenAI(HttpStatusCode.Created, "{}");

        var error = await Record.ExceptionAsync(() =>
            CreateClient(fake, ("GptLive:IdleTimeoutSeconds", "120"))
                .PostAsync("/live/session", new StringContent(Offer)));

        Assert.NotNull(error);
        Assert.Contains("IdleTimeoutSeconds", error!.ToString());
    }
}
