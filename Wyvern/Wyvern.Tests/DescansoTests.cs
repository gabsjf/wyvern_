using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;

namespace Wyvern.Tests;

public class DescansoTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public DescansoTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task EntrarNaCampanhaAsync(HttpClient mestre, HttpClient jogador, int campanhaId)
    {
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();
    }

    /// <summary>Personagem nível 5 com 40 PV (10 atuais), 5d10 de dados de vida e 4 espaços de 1º nível, 3 gastos (sobra 1).</summary>
    private static async Task<int> CriarPersonagemAsync(HttpClient cliente, int campanhaId)
    {
        var resposta = await cliente.PostAsJsonAsync("/Personagem", new
        {
            nome = "Thorin", campanhaId, tipoId = 1,
            criadoPorId = 1, // a tela de criação mandava um valor fixo; o servidor tem que ignorá-lo
            personagemPlayer = new { classe = "Guerreiro", nivel = 5, raca = "Anão" },
            personagemCombate = new { vidaMaxima = 40, vidaAtual = 10, classeArmadura = 16, dadoVidaMaximo = "5d10", dadoVidaGasto = 3, vidaTemporaria = 5, deathSaveFalhas = 2 },
            personagemConjuracao = new { atributoConjuracao = "Inteligencia", slotsNivel1Max = 4, slotsNivel1Atual = 1 },
        });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
    }

    private static object Descanso(string tipo = "Curto", int vida = 25, int dadosGastos = 4, bool restaurar = false, int temp = 5, int falhas = 2) => new
    {
        tipo, vidaAtual = vida, vidaTemporaria = temp, dadoVidaGasto = dadosGastos,
        deathSaveSucessos = 0, deathSaveFalhas = falhas, restaurarEspacosMagia = restaurar,
    };

    [Fact]
    public async Task Dono_registra_descanso_curto_e_a_ficha_guarda_o_resultado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var id = await CriarPersonagemAsync(jogador, campanhaId);

        var resposta = await jogador.PostAsJsonAsync($"/Personagem/{id}/descanso", Descanso("Curto", vida: 25, dadosGastos: 4));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var ficha = await jogador.GetFromJsonAsync<JsonElement>($"/Personagem/{id}");
        var combate = ficha.GetProperty("personagemCombate");
        Assert.Equal(25, combate.GetProperty("vidaAtual").GetInt32());
        Assert.Equal(4, combate.GetProperty("dadoVidaGasto").GetInt32());
        // quem criou é quem estava logado, e não o valor que o cliente mandou
        Assert.Equal(sessaoJogador.UsuarioId, ficha.GetProperty("criadoPorId").GetInt32());
    }

    [Fact]
    public async Task Mestre_registra_descanso_de_personagem_de_jogador()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var id = await CriarPersonagemAsync(jogador, campanhaId);

        var resposta = await mestre.PostAsJsonAsync($"/Personagem/{id}/descanso", Descanso("Longo", vida: 40, dadosGastos: 1, restaurar: true, temp: 0, falhas: 0));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Outro_jogador_e_quem_nao_participa_nao_enxergam_nem_descansam_o_personagem_alheio()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (dono, _) = await _factory.NovoUsuarioAsync();
        var (colega, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, dono, campanhaId);
        await EntrarNaCampanhaAsync(mestre, colega, campanhaId);
        var id = await CriarPersonagemAsync(dono, campanhaId);

        // como no resto da API, para quem não é o dono nem o Mestre o personagem simplesmente não existe
        Assert.Equal(HttpStatusCode.NotFound, (await colega.PostAsJsonAsync($"/Personagem/{id}/descanso", Descanso())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.PostAsJsonAsync($"/Personagem/{id}/descanso", Descanso())).StatusCode);

        var ficha = await dono.GetFromJsonAsync<JsonElement>($"/Personagem/{id}");
        Assert.Equal(10, ficha.GetProperty("personagemCombate").GetProperty("vidaAtual").GetInt32());
    }

    [Fact]
    public async Task Descanso_longo_restaura_todos_os_espacos_de_magia()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var id = await CriarPersonagemAsync(mestre, campanhaId);

        (await mestre.PostAsJsonAsync($"/Personagem/{id}/descanso", Descanso("Longo", vida: 40, dadosGastos: 1, restaurar: true, temp: 0, falhas: 0))).EnsureSuccessStatusCode();

        var conjuracao = (await mestre.GetFromJsonAsync<JsonElement>($"/Personagem/{id}")).GetProperty("personagemConjuracao");
        Assert.Equal(4, conjuracao.GetProperty("slotsNivel1Atual").GetInt32());
    }

    [Fact]
    public async Task Sem_restaurar_os_espacos_de_magia_ficam_como_estavam()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var id = await CriarPersonagemAsync(mestre, campanhaId);

        (await mestre.PostAsJsonAsync($"/Personagem/{id}/descanso", Descanso("Curto", restaurar: false))).EnsureSuccessStatusCode();

        var conjuracao = (await mestre.GetFromJsonAsync<JsonElement>($"/Personagem/{id}")).GetProperty("personagemConjuracao");
        Assert.Equal(1, conjuracao.GetProperty("slotsNivel1Atual").GetInt32()); // continua com os 3 gastos
    }

    [Theory]
    [InlineData("Curto", 41, 3, "acima da vida máxima")]
    [InlineData("Curto", -1, 3, "PV negativos")]
    [InlineData("Curto", 20, 6, "mais dados de vida do que o personagem tem")]
    [InlineData("Curto", 20, -1, "dados de vida negativos")]
    [InlineData("Meio", 20, 3, "tipo inexistente")]
    public async Task Valores_fora_dos_limites_da_ficha_sao_recusados(string tipo, int vida, int dadosGastos, string motivo)
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var id = await CriarPersonagemAsync(mestre, campanhaId);

        var resposta = await mestre.PostAsJsonAsync($"/Personagem/{id}/descanso", Descanso(tipo, vida, dadosGastos));

        Assert.True(resposta.StatusCode == HttpStatusCode.BadRequest, $"esperava 400 para {motivo}, veio {(int)resposta.StatusCode}");
    }

    [Fact]
    public async Task Personagem_inexistente_devolve_404()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await mestre.PostAsJsonAsync("/Personagem/999999/descanso", Descanso())).StatusCode);
    }

    [Theory]
    [InlineData("5d10", 5, 5)]
    [InlineData("1d8", 5, 1)]
    [InlineData("d10", 3, 3)] // sem quantidade, vale o nível
    [InlineData("texto qualquer", 4, 4)]
    [InlineData(null, 2, 2)]
    [InlineData("", 6, 6)]
    public void Total_de_dados_de_vida_vem_do_texto_ou_do_nivel(string? texto, int nivel, int esperado)
    {
        var personagem = new Personagem
        {
            PersonagemPlayer = new PersonagemPlayer { Nivel = nivel },
            PersonagemCombate = new PersonagemCombate { DadoVidaMaximo = texto },
        };

        Assert.Equal(esperado, DescansoService.TotalDadosVida(personagem));
    }
}
