using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Wyvern.Application.DTOs.Atributo;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Application.Services;

namespace Wyvern.Tests;

/// <summary>WYV-35: o que cada tipo de personagem precisa ter para ser criado (regras do D&amp;D 2024).</summary>
public class RegrasDaFichaTests
{
    private static CreateAtributoDto Atributos(int valor = 10) => new()
    {
        Forca = valor, Destreza = valor, Constituicao = valor, Inteligencia = valor, Sabedoria = valor, Carisma = valor,
    };

    private static PersonagemCombateUpdateDto Combate() => new() { VidaMaxima = 12, ClasseArmadura = 15, Deslocamento = "9m" };

    private static PersonagemCreateDto HeroiCompleto() => new()
    {
        Nome = "Lyra", CampanhaId = 1, TipoId = 1,
        PersonagemPlayer = new PersonagemPlayerUpdateDto { Classe = "Maga", Raca = "Elfa", Antecedente = "Sábio", Nivel = 1 },
        Atributo = Atributos(),
        PersonagemCombate = Combate(),
    };

    private static PersonagemCreateDto MonstroCompleto() => new()
    {
        Nome = "Goblin", CampanhaId = 1, TipoId = 3,
        PersonagemNpc = new PersonagemNpcDto { Tamanho = "Pequeno", TipoCriatura = "Humanoide", NivelDesafio = "1/4" },
        Atributo = Atributos(),
        PersonagemCombate = Combate(),
    };

    [Fact]
    public void Heroi_completo_pode_ser_criado()
    {
        Assert.Empty(RegrasDaFicha.FaltasParaCriar(HeroiCompleto(), 1));
    }

    [Fact]
    public void Heroi_sem_a_origem_nem_atributos_diz_tudo_o_que_falta()
    {
        var faltas = RegrasDaFicha.FaltasParaCriar(new PersonagemCreateDto { Nome = "Lyra", TipoId = 1 }, 1);

        Assert.Contains("Escolha a classe.", faltas);
        Assert.Contains("Escolha a espécie.", faltas);
        Assert.Contains("Escolha o antecedente.", faltas);
        Assert.Contains("Preencha os seis atributos.", faltas);
        Assert.Contains("Informe os pontos de vida máximos.", faltas);
        Assert.Contains("Informe a classe de armadura (CA).", faltas);
        Assert.Contains("Informe o deslocamento.", faltas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Nivel_fora_de_1_a_20_e_recusado(int nivel)
    {
        var dto = HeroiCompleto();
        dto.PersonagemPlayer!.Nivel = nivel;
        Assert.Contains("O nível vai de 1 a 20.", RegrasDaFicha.FaltasParaCriar(dto, 1));
    }

    [Fact]
    public void Subclasse_so_e_exigida_a_partir_do_nivel_3()
    {
        var dto = HeroiCompleto();
        dto.PersonagemPlayer!.Nivel = 2;
        Assert.Empty(RegrasDaFicha.FaltasParaCriar(dto, 1));

        dto.PersonagemPlayer.Nivel = 3;
        Assert.Contains("A partir do nível 3, escolha a subclasse.", RegrasDaFicha.FaltasParaCriar(dto, 1));

        dto.PersonagemPlayer.Subclasse = "Evocação";
        Assert.Empty(RegrasDaFicha.FaltasParaCriar(dto, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Atributo_fora_de_1_a_30_e_recusado(int valor)
    {
        var dto = HeroiCompleto();
        dto.Atributo = Atributos(valor);
        Assert.Contains("Cada atributo vai de 1 a 30.", RegrasDaFicha.FaltasParaCriar(dto, 1));
    }

    [Fact]
    public void Npc_narrativo_so_precisa_do_nome()
    {
        var narrativo = new PersonagemCreateDto { Nome = "Taverneiro", TipoId = 2, PersonagemNpc = new PersonagemNpcDto { CategoriaUso = "Narrativo" } };
        Assert.Empty(RegrasDaFicha.FaltasParaCriar(narrativo, 2));
        // sem bloco nenhum também é narrativo
        Assert.Empty(RegrasDaFicha.FaltasParaCriar(new PersonagemCreateDto { Nome = "Guarda" }, 2));
        Assert.Contains("Dê um nome ao personagem.", RegrasDaFicha.FaltasParaCriar(new PersonagemCreateDto { Nome = "  " }, 2));
    }

    [Theory]
    [InlineData("Inimigo")]
    [InlineData("Vilao")]
    public void Npc_de_combate_e_vilao_precisam_do_bloco_de_estatisticas(string categoria)
    {
        var npc = new PersonagemCreateDto { Nome = "Capitão Vex", TipoId = 2, PersonagemNpc = new PersonagemNpcDto { CategoriaUso = categoria } };

        var faltas = RegrasDaFicha.FaltasParaCriar(npc, 2);

        Assert.Contains("Informe o tamanho.", faltas);
        Assert.Contains("Informe o tipo de criatura.", faltas);
        Assert.Contains("Escolha o nível de desafio (ND).", faltas);
        Assert.Contains("Preencha os seis atributos.", faltas);
    }

    [Fact]
    public void Monstro_completo_pode_ser_criado_e_nd_invalido_nao()
    {
        Assert.Empty(RegrasDaFicha.FaltasParaCriar(MonstroCompleto(), 3));
        var dto = MonstroCompleto();
        dto.PersonagemNpc!.NivelDesafio = "40";
        Assert.Contains("Escolha o nível de desafio (ND).", RegrasDaFicha.FaltasParaCriar(dto, 3));
    }

    [Theory]
    [InlineData("0", 10)]
    [InlineData("1/4", 50)]
    [InlineData("5", 1800)]
    [InlineData("30", 155000)]
    public void Xp_vem_da_tabela_do_nd(string nd, int xp)
    {
        Assert.Equal(xp, RegrasDaFicha.XpDoNd(nd));
        Assert.Null(RegrasDaFicha.XpDoNd("abc"));
    }
}

/// <summary>A mesma regra aplicada pela API ao criar.</summary>
public class CriacaoDePersonagemTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public CriacaoDePersonagemTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var r = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5e" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    [Fact]
    public async Task Heroi_incompleto_e_recusado_com_a_lista_do_que_falta()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var resposta = await mestre.PostAsJsonAsync("/Personagem", new { nome = "Lyra", campanhaId, tipoId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var erros = corpo.GetProperty("erros").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("Escolha a classe.", erros);
        Assert.Contains("Preencha os seis atributos.", erros);
    }

    [Fact]
    public async Task Heroi_completo_e_npc_narrativo_so_com_o_nome_sao_criados()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        Assert.Equal(HttpStatusCode.Created, (await mestre.PostAsJsonAsync("/Personagem", Fichas.Heroi("Lyra", campanhaId))).StatusCode);
        var npc = await mestre.PostAsJsonAsync("/Personagem", new { nome = "Taverneiro", campanhaId, tipoId = 2, personagemNpc = new { categoriaUso = "Narrativo" } });
        Assert.Equal(HttpStatusCode.Created, npc.StatusCode);
    }

    [Fact]
    public async Task Vilao_sem_bloco_de_estatisticas_e_recusado_e_com_ele_ganha_o_xp_do_nd()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var incompleto = await mestre.PostAsJsonAsync("/Personagem", new { nome = "Capitão Vex", campanhaId, tipoId = 2, personagemNpc = new { categoriaUso = "Vilao" } });
        Assert.Equal(HttpStatusCode.BadRequest, incompleto.StatusCode);

        var completo = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Capitão Vex", campanhaId, tipoId = 2,
            personagemNpc = new { categoriaUso = "Vilao", tamanho = "Médio", tipoCriatura = "Humanoide", nivelDesafio = "5" },
            atributo = Fichas.Atributos, personagemCombate = Fichas.Combate,
        });
        Assert.Equal(HttpStatusCode.Created, completo.StatusCode);
        var criado = await completo.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Vilao", criado.GetProperty("personagemNpc").GetProperty("categoriaUso").GetString());
        Assert.Equal(1800, criado.GetProperty("personagemNpc").GetProperty("xpConcedido").GetInt32());
    }

    [Fact]
    public async Task Xp_informado_pelo_mestre_e_mantido()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var r = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Ogro", campanhaId, tipoId = 3,
            personagemNpc = new { tamanho = "Grande", tipoCriatura = "Gigante", nivelDesafio = "2", xpConcedido = 500 },
            atributo = Fichas.Atributos, personagemCombate = Fichas.Combate,
        });

        Assert.Equal(500, (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemNpc").GetProperty("xpConcedido").GetInt32());
    }

    [Fact]
    public async Task Jogador_que_tenta_criar_npc_cai_nas_regras_de_heroi()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();

        // só o nome passaria como NPC narrativo; como jogador, vira herói e precisa da ficha de herói
        var resposta = await jogador.PostAsJsonAsync("/Personagem", new { nome = "Quer ser NPC", campanhaId, tipoId = 2, personagemNpc = new { categoriaUso = "Narrativo" } });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
