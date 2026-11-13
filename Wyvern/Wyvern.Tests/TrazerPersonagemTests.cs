using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;

namespace Wyvern.Tests;

/// <summary>
/// O jogador traz um herói que já tem para outra campanha (ao entrar por convite): vai uma cópia descansada,
/// com o mesmo nível, e a original fica onde estava.
/// </summary>
public class TrazerPersonagemTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public TrazerPersonagemTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre, string nome)
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome, sistema = "D&D 5e" });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task EntrarAsync(HttpClient mestre, HttpClient jogador, int campanhaId)
    {
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();
    }

    private static async Task<int> CriarHeroiAsync(HttpClient dono, int campanhaId, string nome = "Lyra", int tipoId = 1)
    {
        var resposta = await dono.PostAsJsonAsync("/Personagem", new
        {
            nome, campanhaId, tipoId,
            personagemNpc = Fichas.BlocoSe(tipoId),
            personagemPlayer = new { classe = "Maga", nivel = 5, raca = "Elfa", antecedente = "Soldado", subclasse = "Campeão" },
            atributo = Fichas.Atributos,
            personagemCombate = new { classeArmadura = 14, vidaMaxima = 30, vidaAtual = 12, vidaTemporaria = 5, dadoVidaMaximo = "5d6", dadoVidaGasto = 3, condicoes = "Envenenado", deslocamento = "9m" },
        });
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
    }

    /// <summary>Um jogador com um herói na mesa de um Mestre e acabando de entrar na mesa de outro.</summary>
    private async Task<(HttpClient jogador, HttpClient mestreNovo, int heroiId, int campanhaAntiga, int campanhaNova)> CenarioAsync()
    {
        var (mestreAntigo, _) = await _factory.NovoUsuarioAsync();
        var (mestreNovo, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var antiga = await CriarCampanhaAsync(mestreAntigo, "Mesa antiga");
        var nova = await CriarCampanhaAsync(mestreNovo, "Mesa nova");
        await EntrarAsync(mestreAntigo, jogador, antiga);
        var heroi = await CriarHeroiAsync(jogador, antiga);
        await EntrarAsync(mestreNovo, jogador, nova);
        return (jogador, mestreNovo, heroi, antiga, nova);
    }

    [Fact]
    public async Task Traz_uma_copia_descansada_com_o_mesmo_nivel_e_a_original_fica()
    {
        var (jogador, mestreNovo, heroiId, antiga, nova) = await CenarioAsync();

        var resposta = await jogador.PostAsJsonAsync($"/Personagem/{heroiId}/trazer", new { campanhaId = nova });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var copia = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(heroiId, copia.GetProperty("personagemId").GetInt32());
        Assert.Equal(nova, copia.GetProperty("campanhaId").GetInt32());
        Assert.Equal("Lyra", copia.GetProperty("nome").GetString());
        Assert.Equal(5, copia.GetProperty("personagemPlayer").GetProperty("nivel").GetInt32());
        var combate = copia.GetProperty("personagemCombate");
        Assert.Equal(30, combate.GetProperty("vidaAtual").GetInt32());
        Assert.Equal(0, combate.GetProperty("vidaTemporaria").GetInt32());
        Assert.Equal(0, combate.GetProperty("dadoVidaGasto").GetInt32());
        Assert.True(string.IsNullOrEmpty(combate.GetProperty("condicoes").GetString()));

        // a original continua na campanha antiga, do jeito que estava
        var original = await jogador.GetFromJsonAsync<JsonElement>($"/Personagem/{heroiId}");
        Assert.Equal(antiga, original.GetProperty("campanhaId").GetInt32());
        Assert.Equal(12, original.GetProperty("personagemCombate").GetProperty("vidaAtual").GetInt32());

        // o Mestre novo vê o herói na mesa dele
        var daMesa = await mestreNovo.GetFromJsonAsync<JsonElement>($"/Personagem/pagina?campanhaId={nova}");
        Assert.Contains(daMesa.GetProperty("itens").EnumerateArray(), p => p.GetProperty("nome").GetString() == "Lyra");
    }

    [Fact]
    public async Task A_chegada_fica_registrada_na_cronica_da_campanha_nova()
    {
        var (jogador, mestreNovo, heroiId, _, nova) = await CenarioAsync();

        (await jogador.PostAsJsonAsync($"/Personagem/{heroiId}/trazer", new { campanhaId = nova })).EnsureSuccessStatusCode();

        var eventos = await mestreNovo.GetFromJsonAsync<JsonElement>($"/Campanha/{nova}/Eventos");
        var chegada = Assert.Single(eventos.EnumerateArray(), e => e.GetProperty("titulo").GetString() == "Lyra chegou à campanha");
        Assert.Contains("Maga de nível 5", chegada.GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task Nao_traz_para_campanha_de_que_nao_participa()
    {
        var (jogador, _, heroiId, _, _) = await CenarioAsync();
        var (outroMestre, _) = await _factory.NovoUsuarioAsync();
        var alheia = await CriarCampanhaAsync(outroMestre, "Mesa alheia");

        var resposta = await jogador.PostAsJsonAsync($"/Personagem/{heroiId}/trazer", new { campanhaId = alheia });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Nao_traz_personagem_de_outra_pessoa_nem_para_a_mesma_campanha()
    {
        var (jogador, mestreNovo, heroiId, antiga, nova) = await CenarioAsync();
        var heroiDoMestre = await CriarHeroiAsync(mestreNovo, nova, "Do Mestre");

        // o jogador não leva o que não é dele, nem "traz" para a campanha onde o herói já está
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.PostAsJsonAsync($"/Personagem/{heroiDoMestre}/trazer", new { campanhaId = antiga })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await jogador.PostAsJsonAsync($"/Personagem/{heroiId}/trazer", new { campanhaId = antiga })).StatusCode);
    }

    [Fact]
    public async Task Monstro_ou_npc_nao_e_trazido_por_aqui()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var a = await CriarCampanhaAsync(mestre, "A");
        var b = await CriarCampanhaAsync(mestre, "B");
        var goblin = await CriarHeroiAsync(mestre, a, "Goblin", tipoId: 3);

        var resposta = await mestre.PostAsJsonAsync($"/Personagem/{goblin}/trazer", new { campanhaId = b });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode); // o Mestre usa o "Duplicar" para criaturas
    }

    [Fact]
    public void Preparar_para_nova_campanha_zera_o_estado_de_jogo_e_a_subida_pendente()
    {
        var p = new Personagem
        {
            PersonagemCombate = new PersonagemCombate { VidaMaxima = 20, VidaAtual = 3, VidaTemporaria = 4, DadoVidaGasto = 2, DeathSaveFalhas = 2, DeathSaveSucessos = 1, InspiracaoHeroica = true, Condicoes = "Caído" },
            PersonagemConjuracao = new PersonagemConjuracao { SlotsNivel1Max = 4, SlotsNivel1Atual = 0, SlotsNivel3Max = 2, SlotsNivel3Atual = 1 },
            PersonagemPlayer = new PersonagemPlayer { Nivel = 6, NivelLiberado = 7 },
        };

        PersonagemClonador.PrepararParaNovaCampanha(p);

        Assert.Equal(20, p.PersonagemCombate.VidaAtual);
        Assert.Equal(0, p.PersonagemCombate.VidaTemporaria + p.PersonagemCombate.DadoVidaGasto + p.PersonagemCombate.DeathSaveFalhas + p.PersonagemCombate.DeathSaveSucessos);
        Assert.False(p.PersonagemCombate.InspiracaoHeroica);
        Assert.Equal(string.Empty, p.PersonagemCombate.Condicoes);
        Assert.Equal(4, p.PersonagemConjuracao.SlotsNivel1Atual);
        Assert.Equal(2, p.PersonagemConjuracao.SlotsNivel3Atual);
        Assert.Equal(6, p.PersonagemPlayer.NivelLiberado);
    }
}
