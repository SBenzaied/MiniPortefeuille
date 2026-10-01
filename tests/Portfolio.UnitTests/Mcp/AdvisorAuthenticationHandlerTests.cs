using System.Net;
using Portfolio.Mcp.Api;

namespace Portfolio.UnitTests.Mcp;

/// <summary>Ajout du jeton et renouvellement sur 401, avec un fournisseur de jeton simulé par Moq.</summary>
[TestFixture]
public class AdvisorAuthenticationHandlerTests
{
    /// <summary>Faux serveur : répond avec les codes donnés, dans l'ordre, et garde les en-têtes reçus.</summary>
    private sealed class ScriptedServer(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        private int _call;
        public List<string?> ReceivedTokens { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ReceivedTokens.Add(request.Headers.Authorization?.Parameter);
            return Task.FromResult(new HttpResponseMessage(statuses[Math.Min(_call++, statuses.Length - 1)]));
        }
    }

    private static HttpClient ClientWith(IAdvisorTokenProvider tokens, HttpMessageHandler server) =>
        new(new AdvisorAuthenticationHandler(tokens) { InnerHandler = server }) { BaseAddress = new Uri("http://api.test") };

    [Test]
    public async Task Every_request_carries_the_advisor_token()
    {
        var tokens = new Mock<IAdvisorTokenProvider>();
        tokens.Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("jeton-1");
        var server = new ScriptedServer(HttpStatusCode.OK);

        var response = await ClientWith(tokens.Object, server).GetAsync("/api/clients");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(server.ReceivedTokens, Is.EqualTo(new[] { "jeton-1" }));
        tokens.Verify(t => t.Invalidate(), Times.Never);
    }

    [Test]
    public async Task A_rejected_token_is_renewed_and_the_request_replayed_once()
    {
        var tokens = new Mock<IAdvisorTokenProvider>();
        tokens.SetupSequence(t => t.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("jeton-perime")
            .ReturnsAsync("jeton-neuf");
        var server = new ScriptedServer(HttpStatusCode.Unauthorized, HttpStatusCode.OK);

        var response = await ClientWith(tokens.Object, server).GetAsync("/api/clients");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(server.ReceivedTokens, Is.EqualTo(new[] { "jeton-perime", "jeton-neuf" }));
        tokens.Verify(t => t.Invalidate(), Times.Once);
    }

    [Test]
    public async Task A_second_refusal_is_returned_without_looping()
    {
        var tokens = new Mock<IAdvisorTokenProvider>();
        tokens.Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("jeton");
        var server = new ScriptedServer(HttpStatusCode.Unauthorized);

        var response = await ClientWith(tokens.Object, server).GetAsync("/api/clients");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(server.ReceivedTokens, Has.Count.EqualTo(2));
    }
}
