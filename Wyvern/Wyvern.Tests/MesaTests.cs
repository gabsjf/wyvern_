using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Text.Json;
using Wyvern.Application.Services;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Tests;

public class MesaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public MesaTests(WyvernApiFactory factory) => _factory = factory;

    private static RolagemEntradaDto Entrada(string rotulo = "Furtividade", string formula = "1d20+5", int total = 17, int[]? dados = null) =>
        new() { Rotulo = rotulo, Formula = formula, Total = total, Dados = dados ?? [12], Personagem = "Lyra" };

    // ---------- regras de validação e histórico (sem rede) ----------

    [Fact]
    public void Rolagem_valida_ganha_autor_e_horario_do_servidor()
    {
        var agora = new DateTime(2026, 11, 2, 20, 0, 0, DateTimeKind.Utc);

        var r = RolagemMesaFactory.Criar(7, "Rafa", Entrada(), agora);

        Assert.Equal((7, "Rafa", agora, 17), (r.UsuarioId, r.Usuario, r.Quando, r.Total));
        Assert.NotEqual(Guid.Empty, r.Id);
    }

    [Theory]
    [InlineData("", "1d20", 10)]
    [InlineData("   ", "1d20", 10)]
    [InlineData("ok", "", 10)]
    [InlineData("ok", "1d20+1d20+1d20+1d20+1d20+1d20+1d20+1d20+1d20+1d20+1d20", 10)] // fórmula longa demais
    [InlineData("ok", "1d20", 200_000)]
    public void Rolagem_invalida_e_recusada(string rotulo, string formula, int total)
    {
        Assert.Throws<RegraNegocioException>(() => RolagemMesaFactory.Criar(1, "Rafa", Entrada(rotulo, formula, total)));
    }

    [Fact]
    public void Dados_impossiveis_ou_em_excesso_sao_recusados()
    {
        Assert.Throws<RegraNegocioException>(() => RolagemMesaFactory.Criar(1, "Rafa", Entrada(dados: [0])));
        Assert.Throws<RegraNegocioException>(() => RolagemMesaFactory.Criar(1, "Rafa", Entrada(dados: [5000])));
        Assert.Throws<RegraNegocioException>(() => RolagemMesaFactory.Criar(1, "Rafa", Entrada(dados: Enumerable.Repeat(3, 61).ToArray())));
    }

    [Fact]
    public void Historico_guarda_so_as_ultimas_e_separa_por_campanha()
    {
        var historico = new HistoricoRolagens();
        for (var i = 1; i <= HistoricoRolagens.Limite + 5; i++)
            historico.Adicionar(1, RolagemMesaFactory.Criar(1, "Rafa", Entrada(total: i)));
        historico.Adicionar(2, RolagemMesaFactory.Criar(1, "Rafa", Entrada(total: 999)));

        var recentes = historico.Recentes(1);

        Assert.Equal(HistoricoRolagens.Limite, recentes.Count);
        Assert.Equal(6, recentes[0].Total); // as 5 primeiras saíram
        Assert.Equal(HistoricoRolagens.Limite + 5, recentes[^1].Total);
        Assert.Single(historico.Recentes(2));
        Assert.Empty(historico.Recentes(3));
    }

    // ---------- o hub de verdade ----------

    private async Task<HubConnection> ConectarAsync(string token)
    {
        var conexao = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/mesahub"), o =>
            {
                o.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                o.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
        await conexao.StartAsync();
        return conexao;
    }

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task EntrarNaCampanhaAsync(HttpClient mestre, HttpClient jogador, int campanhaId)
    {
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Rolagem_de_um_membro_chega_aos_outros_e_fica_no_historico()
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);

        await using var hubMestre = await ConectarAsync(sessaoMestre.Token);
        await using var hubJogador = await ConectarAsync(sessaoJogador.Token);
        var recebida = new TaskCompletionSource<RolagemMesa>();
        hubMestre.On<RolagemMesa>("ReceberRolagem", r => recebida.TrySetResult(r));

        await hubMestre.InvokeAsync<List<RolagemMesa>>("EntrarNaMesa", campanhaId);
        await hubJogador.InvokeAsync<List<RolagemMesa>>("EntrarNaMesa", campanhaId);
        await hubJogador.InvokeAsync("Rolar", campanhaId, Entrada("Ataque: Espada", "1d20+7", 19, [12]));

        var r = await recebida.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Ataque: Espada", r.Rotulo);
        Assert.Equal(19, r.Total);
        Assert.Equal(sessaoJogador.UsuarioId, r.UsuarioId); // o autor vem do token, não do que o cliente diz

        // quem entra depois vê a rolagem no histórico
        await using var hubTardio = await ConectarAsync(sessaoMestre.Token);
        var historico = await hubTardio.InvokeAsync<List<RolagemMesa>>("EntrarNaMesa", campanhaId);
        Assert.Contains(historico, h => h.Total == 19);
    }

    [Fact]
    public async Task Quem_nao_participa_da_campanha_nao_entra_nem_rola()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (_, sessaoEstranho) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        await using var hub = await ConectarAsync(sessaoEstranho.Token);

        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync<List<RolagemMesa>>("EntrarNaMesa", campanhaId));
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("Rolar", campanhaId, Entrada()));
    }

    [Fact]
    public async Task Rolagem_invalida_volta_como_erro_do_hub()
    {
        var (mestre, sessao) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await using var hub = await ConectarAsync(sessao.Token);
        await hub.InvokeAsync<List<RolagemMesa>>("EntrarNaMesa", campanhaId);

        var erro = await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("Rolar", campanhaId, Entrada(rotulo: "")));

        Assert.Contains("rótulo", erro.Message);
    }
}
