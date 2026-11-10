using Microsoft.Extensions.Configuration;
using Wyvern.Api.Extensions;

namespace Wyvern.Tests;

/// <summary>O que vai para o Sentry não pode levar tokens que dão acesso (links de e-mail, convites, refresh).</summary>
public class SentryTests
{
    [Theory]
    [InlineData("https://wyvern.sbs/confirmar-email?token=12.abcDEF", "https://wyvern.sbs/confirmar-email?token=[removido]")]
    [InlineData("/Auth/redefinir?x=1&token=9.zzz&y=2", "/Auth/redefinir?x=1&token=[removido]&y=2")]
    [InlineData("?refreshToken=abc", "?refreshToken=[removido]")]
    [InlineData("/Campanha/join/0f1e2d3c4b5a?x=1", "/Campanha/join/[removido]?x=1")]
    [InlineData("/Campanha/3/membros", "/Campanha/3/membros")]
    public void Remove_tokens_de_urls(string entrada, string esperado) =>
        Assert.Equal(esperado, SentryConfiguracao.LimparTexto(entrada));

    [Fact]
    public void Sem_dsn_o_sentry_fica_desligado()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Sentry:Dsn"] = "" })
            .Build();
        Assert.False(SentryConfiguracao.Ativo(config));
    }
}
