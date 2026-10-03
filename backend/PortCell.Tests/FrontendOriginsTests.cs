using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PortCell.Api;

namespace PortCell.Tests;

public class FrontendOriginsTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Theory]
    [InlineData("http://26.245.9.1:5173", true)]
    [InlineData("http://localhost:5173", true)]
    [InlineData("http://127.0.0.1:5173", true)]
    [InlineData("http://26.245.9.2:5173", false)]
    [InlineData("http://26.245.9.1:5174", false)]
    [InlineData("http://localhost:5173.evil.test", false)]
    [InlineData("null", false)]
    public void RenovacaoDeSessaoAceitaSomenteOrigensConfiguradas(string origin, bool allowed)
    {
        var config = Config(new()
        {
            ["Frontend:Origin"] = "http://26.245.9.1:5173",
            ["Frontend:AllowedOrigins:0"] = "http://localhost:5173",
            ["Frontend:AllowedOrigins:1"] = "http://127.0.0.1:5173"
        });
        var context = new DefaultHttpContext();
        context.Request.Headers.Origin = origin;
        Assert.Equal(allowed, AuthTokens.OriginAllowed(context, config));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("http://localhost:5173/path")]
    [InlineData("http://localhost:5173?redirect=evil")]
    [InlineData("http://user:password@localhost:5173")]
    public void ConfiguracaoRecusaOrigensAmplasOuComCaminhos(string origin) =>
        Assert.Throws<InvalidOperationException>(() => FrontendOrigins.Allowed(Config(new() { ["Frontend:Origin"] = origin })));
}
