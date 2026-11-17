using System.Net.Http.Json;
using System.Text.Json;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;

namespace Wyvern.Tests;

/// <summary>O herói em combate e a ficha dele mostram sempre o mesmo PV, CA, condições e salvaguardas de morte.</summary>
public class SincronizacaoDeCombateTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public SincronizacaoDeCombateTests(WyvernApiFactory factory) => _factory = factory;

    // ---------- a regra, sem rede ----------

    [Fact]
    public void Dano_do_combate_passa_primeiro_pelo_pv_temporario_da_ficha()
    {
        var ficha = new PersonagemCombate { VidaAtual = 30, VidaMaxima = 40, VidaTemporaria = 5, ClasseArmadura = 14 };
        var participante = new CombateParticipante { VidaAtual = 22, Condicoes = "Envenenado", FalhasMorte = 1 }; // era 30: 8 de dano

        SincronizacaoDeCombate.LevarParaFicha(participante, vidaAnterior: 30, ficha);

        Assert.Equal((27, 0), (ficha.VidaAtual, ficha.VidaTemporaria));
        Assert.Equal(27, participante.VidaAtual); // o combate fica com o PV certo
        Assert.Equal(("Envenenado", 1), (ficha.Condicoes, ficha.DeathSaveFalhas));
    }

    [Fact]
    public void Cura_do_combate_nao_passa_do_maximo_da_ficha()
    {
        var ficha = new PersonagemCombate { VidaAtual = 38, VidaMaxima = 40 };
        var participante = new CombateParticipante { VidaAtual = 48 };

        SincronizacaoDeCombate.LevarParaFicha(participante, vidaAnterior: 38, ficha);

        Assert.Equal(40, ficha.VidaAtual);
        Assert.Equal(40, participante.VidaAtual);
    }

    [Fact]
    public void Copiar_da_ficha_diz_se_mudou_algo()
    {
        var ficha = new PersonagemCombate { VidaAtual = 10, VidaMaxima = 12, ClasseArmadura = 15, Condicoes = "Caído" };
        var participante = new CombateParticipante();

        Assert.True(SincronizacaoDeCombate.CopiarDaFicha(ficha, participante));
        Assert.False(SincronizacaoDeCombate.CopiarDaFicha(ficha, participante));
        Assert.Equal((10, 12, 15, "Caído"), (participante.VidaAtual, participante.VidaMaxima, participante.ClasseArmadura, participante.Condicoes));
    }

    // ---------- pela API ----------

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task<int> CriarSessaoAsync(HttpClient mestre, int campanhaId)
    {
        var resposta = await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = 1, nome = "S1", dataSessao = DateTime.Today, obs = "", campanhaId });
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();
    }

    /// <summary>40 PV (30 atuais), CA 14, Caído.</summary>
    private static async Task<int> CriarPersonagemAsync(HttpClient cliente, int campanhaId, int tipoId = 1)
    {
        var resposta = await cliente.PostAsJsonAsync("/Personagem", new
        {
            nome = tipoId == 1 ? "Lyra" : "Ogro", campanhaId, tipoId,
            personagemPlayer = tipoId == 1 ? Fichas.Origem : null,
            atributo = Fichas.Atributos,
            personagemCombate = new { classeArmadura = 14, vidaMaxima = 40, vidaAtual = 30, condicoes = "Caído", deslocamento = "9m" },
            personagemNpc = Fichas.BlocoSe(tipoId),
        });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
    }

    /// <summary>Começa o combate mandando números errados de propósito: o servidor tem que usar os da ficha.</summary>
    private static async Task<int> ComecarCombateAsync(HttpClient mestre, int sessaoId, int personagemId, bool inimigo = false)
    {
        var resposta = await mestre.PostAsJsonAsync("/Combate/start", new
        {
            sessaoId,
            participantes = new object[]
            {
                new { personagemId, nomeNPC = "X", iniciativa = 15, vidaAtual = 99, vidaMaxima = 99, classeArmadura = 1, isInimigo = inimigo, condicoes = "" },
                new { nomeNPC = "Goblin", iniciativa = 10, vidaAtual = 7, vidaMaxima = 7, classeArmadura = 13, isInimigo = true, condicoes = "" },
            },
        });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("combateId").GetInt32();
    }

    private static async Task<JsonElement> ParticipanteAsync(HttpClient cliente, int combateId, int personagemId) =>
        (await cliente.GetFromJsonAsync<JsonElement>($"/Combate/{combateId}/participantes"))
            .EnumerateArray().First(p => p.TryGetProperty("personagemId", out var id) && id.ValueKind == JsonValueKind.Number && id.GetInt32() == personagemId);

    private static async Task<JsonElement> FichaAsync(HttpClient cliente, int personagemId) =>
        (await cliente.GetFromJsonAsync<JsonElement>($"/Personagem/{personagemId}")).GetProperty("personagemCombate");

    [Fact]
    public async Task Heroi_entra_no_combate_com_o_que_esta_na_ficha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var heroi = await CriarPersonagemAsync(mestre, campanhaId);

        var combateId = await ComecarCombateAsync(mestre, await CriarSessaoAsync(mestre, campanhaId), heroi);

        var p = await ParticipanteAsync(mestre, combateId, heroi);
        Assert.Equal((30, 40, 14, "Caído"),
            (p.GetProperty("vidaAtual").GetInt32(), p.GetProperty("vidaMaxima").GetInt32(), p.GetProperty("classeArmadura").GetInt32(), p.GetProperty("condicoes").GetString()));
    }

    [Fact]
    public async Task Dano_e_condicao_no_combate_chegam_na_ficha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var heroi = await CriarPersonagemAsync(mestre, campanhaId);
        (await mestre.PutAsJsonAsync($"/Personagem/{heroi}/estado", new { vidaTemporaria = 5 })).EnsureSuccessStatusCode();
        var combateId = await ComecarCombateAsync(mestre, await CriarSessaoAsync(mestre, campanhaId), heroi);
        var participanteId = (await ParticipanteAsync(mestre, combateId, heroi)).GetProperty("participanteId").GetInt32();

        // o Mestre tira 8 no rastreador e marca Envenenado
        (await mestre.PutAsJsonAsync($"/Combate/{combateId}/participantes/{participanteId}",
            new { vidaAtual = 22, condicoes = "Envenenado", sucessosMorte = 0, falhasMorte = 0 })).EnsureSuccessStatusCode();

        var ficha = await FichaAsync(mestre, heroi);
        Assert.Equal((27, 0, "Envenenado"), (ficha.GetProperty("vidaAtual").GetInt32(), ficha.GetProperty("vidaTemporaria").GetInt32(), ficha.GetProperty("condicoes").GetString()));
        Assert.Equal(27, (await ParticipanteAsync(mestre, combateId, heroi)).GetProperty("vidaAtual").GetInt32());
    }

    [Fact]
    public async Task Jogador_muda_a_propria_ficha_e_o_combate_acompanha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();
        var heroi = await CriarPersonagemAsync(jogador, campanhaId);
        var combateId = await ComecarCombateAsync(mestre, await CriarSessaoAsync(mestre, campanhaId), heroi);

        (await jogador.PutAsJsonAsync($"/Personagem/{heroi}/estado", new { vidaAtual = 12, condicoes = "Amedrontado", deathSaveFalhas = 2 })).EnsureSuccessStatusCode();

        var p = await ParticipanteAsync(mestre, combateId, heroi);
        Assert.Equal((12, "Amedrontado", 2),
            (p.GetProperty("vidaAtual").GetInt32(), p.GetProperty("condicoes").GetString(), p.GetProperty("falhasMorte").GetInt32()));
    }

    [Fact]
    public async Task Monstro_no_combate_nao_mexe_na_ficha_dele()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var ogro = await CriarPersonagemAsync(mestre, campanhaId, tipoId: 3);
        var combateId = await ComecarCombateAsync(mestre, await CriarSessaoAsync(mestre, campanhaId), ogro, inimigo: true);
        var participante = await ParticipanteAsync(mestre, combateId, ogro);
        Assert.Equal(99, participante.GetProperty("vidaAtual").GetInt32()); // o molde não manda no combatente

        (await mestre.PutAsJsonAsync($"/Combate/{combateId}/participantes/{participante.GetProperty("participanteId").GetInt32()}",
            new { vidaAtual = 3, condicoes = "Caído", sucessosMorte = 0, falhasMorte = 0 })).EnsureSuccessStatusCode();

        Assert.Equal(30, (await FichaAsync(mestre, ogro)).GetProperty("vidaAtual").GetInt32());
    }
}
