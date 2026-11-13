using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Tests;

/// <summary>Regras de subida de nível do D&D 2024 (tabelas e aplicação na ficha).</summary>
public class RegrasDeNivelTests
{
    [Theory]
    [InlineData(0, 1)] [InlineData(299, 1)] [InlineData(300, 2)] [InlineData(6500, 5)] [InlineData(354999, 19)] [InlineData(355000, 20)] [InlineData(999999, 20)]
    public void Nivel_pelo_xp_segue_a_tabela(int xp, int nivel) => Assert.Equal(nivel, RegrasDnd2024.NivelPeloXp(xp));

    [Theory]
    [InlineData(1, 2)] [InlineData(4, 2)] [InlineData(5, 3)] [InlineData(9, 4)] [InlineData(13, 5)] [InlineData(17, 6)] [InlineData(20, 6)]
    public void Proficiencia_por_nivel(int nivel, int bonus) => Assert.Equal(bonus, RegrasDnd2024.Proficiencia(nivel));

    [Theory]
    [InlineData("Guerreiro", "Guerreiro")] [InlineData("fighter", "Guerreiro")] [InlineData("Clérigo (Vida)", "Clérigo")]
    [InlineData("CLERIGO", "Clérigo")] [InlineData("Ranger", "Patrulheiro")] [InlineData("Bruxo/Feiticeiro", "Bruxo")]
    public void Acha_a_classe_pelo_texto_da_ficha(string texto, string nome) => Assert.Equal(nome, RegrasDnd2024.AcharClasse(texto)!.Nome);

    [Fact]
    public void Classe_fora_do_livro_nao_e_reconhecida() => Assert.Null(RegrasDnd2024.AcharClasse("Artífice das Sombras"));

    [Fact]
    public void Espacos_de_magia_por_tipo_de_conjurador()
    {
        Assert.Equal(new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 }, RegrasDnd2024.Espacos(TipoConjurador.Completo, 5));
        Assert.Equal(new[] { 2, 0, 0, 0, 0, 0, 0, 0, 0 }, RegrasDnd2024.Espacos(TipoConjurador.Meio, 1)); // novidade do 2024
        Assert.Equal(new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 }, RegrasDnd2024.Espacos(TipoConjurador.Meio, 20));
        Assert.Equal(new[] { 0, 0, 2, 0, 0, 0, 0, 0, 0 }, RegrasDnd2024.Espacos(TipoConjurador.Pacto, 5));
        Assert.Equal(new[] { 4, 3, 3, 3, 3, 2, 2, 1, 1 }, RegrasDnd2024.Espacos(TipoConjurador.Completo, 20));
    }

    [Fact]
    public void Guerreiro_e_ladino_tem_melhorias_a_mais()
    {
        Assert.Contains(RegrasDnd2024.Melhoria, RegrasDnd2024.CaracteristicasDoNivel(RegrasDnd2024.Classes["guerreiro"], 6));
        Assert.Contains(RegrasDnd2024.Melhoria, RegrasDnd2024.CaracteristicasDoNivel(RegrasDnd2024.Classes["ladino"], 10));
        Assert.DoesNotContain(RegrasDnd2024.Melhoria, RegrasDnd2024.CaracteristicasDoNivel(RegrasDnd2024.Classes["mago"], 6));
        Assert.Contains(RegrasDnd2024.DadivaEpica, RegrasDnd2024.CaracteristicasDoNivel(RegrasDnd2024.Classes["mago"], 19));
    }

    private static Personagem Ficha(string classe = "Guerreiro", int nivel = 3, int xp = 900, int liberado = 0, int con = 14, int vida = 28) => new()
    {
        PersonagemId = 1, Nome = "Thorin", TipoId = 1,
        PersonagemPlayer = new PersonagemPlayer { Classe = classe, Raca = "Anão", Alinhamento = "", Nivel = nivel, Xp = xp, NivelLiberado = liberado, Subclasse = nivel >= 3 ? "Campeão" : null },
        PersonagemCombate = new PersonagemCombate { VidaMaxima = vida, VidaAtual = 10, ProficienciaBonus = 2, DadoVidaMaximo = $"{nivel}d10" },
        Atributo = new Atributo { Forca = 16, Destreza = 12, Constituicao = con, Inteligencia = 10, Sabedoria = 10, Carisma = 8 },
    };

    [Fact]
    public void Sem_xp_nem_marco_nao_sobe()
    {
        var previa = ProgressaoService.Previa(Ficha(xp: 900));
        Assert.False(previa.PodeSubir);
        Assert.Contains("Mestre", previa.Motivo);
        Assert.Throws<RegraNegocioException>(() => ProgressaoService.Aplicar(Ficha(xp: 900), new SubirNivelDto()));
    }

    [Fact]
    public void Previa_do_guerreiro_no_nivel_4_pede_melhoria()
    {
        var previa = ProgressaoService.Previa(Ficha(xp: 6500));
        Assert.True(previa.PodeSubir);
        Assert.Equal(4, previa.ProximoNivel);
        Assert.Equal(10, previa.DadoVida);
        Assert.Equal(6, previa.PvFixo);
        Assert.True(previa.MelhoriaDeAtributo);
        Assert.False(previa.EscolheSubclasse);
        Assert.Contains(previa.Recursos, r => r.Nome == "Maestria em Armas" && r.Antes == "3" && r.Depois == "4");
    }

    [Fact]
    public void Subida_com_pv_fixo_e_con_que_sobe_ajusta_os_pv_de_todos_os_niveis()
    {
        var p = Ficha(xp: 900, liberado: 4, con: 15); // CON 15 (+2) → 16 (+3)
        var (pv, resumo) = ProgressaoService.Aplicar(p, new SubirNivelDto
        {
            ModoPv = "Fixo",
            Melhoria = new MelhoriaEscolhidaDto { Tipo = "Atributos", Aumentos = new() { ["constituicao"] = 1, ["forca"] = 1 } },
        });

        // 6 (fixo d10) + 2 (CON antiga) + 1 por nível × 4 níveis
        Assert.Equal(12, pv);
        Assert.Equal(40, p.PersonagemCombate!.VidaMaxima);
        Assert.Equal(22, p.PersonagemCombate.VidaAtual);
        Assert.Equal(4, p.PersonagemPlayer!.Nivel);
        Assert.Equal("4d10", p.PersonagemCombate.DadoVidaMaximo);
        Assert.Equal(16, p.Atributo!.Constituicao);
        Assert.Equal(17, p.Atributo.Forca);
        Assert.Contains("Aprimoramento de Atributo", p.PersonagemDetalhes!.Talentos);
        Assert.Contains("nível 4", resumo);
    }

    [Theory]
    [InlineData(0)] [InlineData(11)]
    public void Pv_rolado_fora_do_dado_e_recusado(int rolado)
    {
        var p = Ficha("Guerreiro", nivel: 4, xp: 6500, liberado: 5);
        Assert.Throws<RegraNegocioException>(() => ProgressaoService.Aplicar(p, new SubirNivelDto { ModoPv = "Rolado", PvRolado = rolado }));
    }

    [Fact]
    public void Pv_por_nivel_nunca_e_menor_que_1()
    {
        var p = Ficha("Mago", nivel: 1, xp: 300, con: 3, vida: 2); // CON -4
        var (pv, _) = ProgressaoService.Aplicar(p, new SubirNivelDto { ModoPv = "Rolado", PvRolado = 1 });
        Assert.Equal(1, pv);
    }

    [Fact]
    public void Melhoria_invalida_e_recusada()
    {
        SubirNivelDto Com(Dictionary<string, int> aumentos) => new() { Melhoria = new MelhoriaEscolhidaDto { Tipo = "Atributos", Aumentos = aumentos } };
        Assert.Throws<RegraNegocioException>(() => ProgressaoService.Aplicar(Ficha(xp: 6500), Com(new() { ["forca"] = 3 })));
        Assert.Throws<RegraNegocioException>(() => ProgressaoService.Aplicar(Ficha(xp: 6500), Com(new() { ["forca"] = 1 })));
        Assert.Throws<RegraNegocioException>(() => ProgressaoService.Aplicar(Ficha(xp: 6500), new SubirNivelDto()));
        var forte = Ficha(xp: 6500); forte.Atributo!.Forca = 20;
        Assert.Throws<RegraNegocioException>(() => ProgressaoService.Aplicar(forte, Com(new() { ["forca"] = 2 })));
    }

    [Fact]
    public void Nivel_3_exige_subclasse_e_mago_ganha_espacos_novos_cheios()
    {
        var p = Ficha("Mago", nivel: 2, xp: 900, con: 12, vida: 10);
        p.PersonagemPlayer!.Subclasse = null;
        p.PersonagemConjuracao = new PersonagemConjuracao { SlotsNivel1Max = 3, SlotsNivel1Atual = 1 };
        Assert.Throws<RegraNegocioException>(() => ProgressaoService.Aplicar(p, new SubirNivelDto()));

        ProgressaoService.Aplicar(p, new SubirNivelDto { Subclasse = "Evocador" });
        Assert.Equal("Evocador", p.PersonagemPlayer.Subclasse);
        Assert.Equal(4, p.PersonagemConjuracao.SlotsNivel1Max);
        Assert.Equal(2, p.PersonagemConjuracao.SlotsNivel1Atual); // ganhou 1 espaço novo, os gastos continuam gastos
        Assert.Equal(2, p.PersonagemConjuracao.SlotsNivel2Max);
        Assert.Equal(2, p.PersonagemConjuracao.SlotsNivel2Atual);
        Assert.Contains("Nível 3: Subclasse de Mago", p.PersonagemDetalhes!.CaracteristicasClasse);
    }

    [Fact]
    public void Classe_fora_do_livro_sobe_so_com_pv_proficiencia_e_dados()
    {
        var p = Ficha("Artífice das Sombras", nivel: 4, xp: 6500);
        var previa = ProgressaoService.Previa(p);
        Assert.False(previa.ClasseReconhecida);
        Assert.True(previa.PodeSubir);
        Assert.Equal(10, previa.DadoVida); // tirado de "4d10"
        ProgressaoService.Aplicar(p, new SubirNivelDto());
        Assert.Equal(3, p.PersonagemCombate!.ProficienciaBonus);
        Assert.Equal("5d10", p.PersonagemCombate.DadoVidaMaximo);
    }
}

/// <summary>Rotas de progressão e a trava dos campos de nível na edição comum.</summary>
public class ProgressaoApiTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;
    public ProgressaoApiTests(WyvernApiFactory factory) => _factory = factory;

    private async Task<(HttpClient mestre, HttpClient jogador, int id)> MesaComHeroiAsync()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var camp = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        var campanhaId = (await camp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();

        var criado = await jogador.PostAsJsonAsync("/Personagem", new
        {
            nome = "Thorin", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Guerreiro", nivel = 1, raca = "Anão", alinhamento = "", antecedente = "Soldado", subclasse = "Campeão" },
            personagemCombate = new { vidaMaxima = 12, vidaAtual = 12, classeArmadura = 16, proficienciaBonus = 2, dadoVidaMaximo = "1d10", deslocamento = "9m" },
            atributo = new { forca = 16, destreza = 12, constituicao = 14, inteligencia = 10, sabedoria = 10, carisma = 8 },
        });
        criado.EnsureSuccessStatusCode();
        var id = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
        return (mestre, jogador, id);
    }

    [Fact]
    public async Task Jogador_nao_libera_nivel_e_so_sobe_depois_do_mestre()
    {
        var (mestre, jogador, id) = await MesaComHeroiAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PutAsJsonAsync($"/Personagem/{id}/progressao", new { xp = 300 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await jogador.PostAsJsonAsync($"/Personagem/{id}/subir-nivel", new { modoPv = "Fixo" })).StatusCode);

        (await mestre.PutAsJsonAsync($"/Personagem/{id}/progressao", new { xp = 300 })).EnsureSuccessStatusCode();
        var previa = await jogador.GetFromJsonAsync<JsonElement>($"/Personagem/{id}/proximo-nivel");
        Assert.True(previa.GetProperty("podeSubir").GetBoolean());

        var resposta = await jogador.PostAsJsonAsync($"/Personagem/{id}/subir-nivel", new { modoPv = "Fixo" });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, resultado.GetProperty("nivelNovo").GetInt32());
        Assert.Equal(8, resultado.GetProperty("pvGanhos").GetInt32()); // 6 + CON +2

        var ficha = await jogador.GetFromJsonAsync<JsonElement>($"/Personagem/{id}");
        Assert.Equal(20, ficha.GetProperty("personagemCombate").GetProperty("vidaMaxima").GetInt32());
        Assert.Equal(2, ficha.GetProperty("personagemPlayer").GetProperty("nivelDisponivel").GetInt32());

        // sem mais nada liberado, não sobe de novo
        Assert.Equal(HttpStatusCode.BadRequest, (await jogador.PostAsJsonAsync($"/Personagem/{id}/subir-nivel", new { modoPv = "Fixo" })).StatusCode);
    }

    [Fact]
    public async Task Marco_do_mestre_libera_sem_xp()
    {
        var (mestre, jogador, id) = await MesaComHeroiAsync();
        (await mestre.PutAsJsonAsync($"/Personagem/{id}/progressao", new { nivelLiberado = 2 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await jogador.PostAsJsonAsync($"/Personagem/{id}/subir-nivel", new { modoPv = "Rolado", pvRolado = 9 })).StatusCode);
    }

    [Fact]
    public async Task Na_edicao_o_jogador_muda_a_historia_mas_nao_os_campos_de_nivel_e_o_mestre_corrige_tudo()
    {
        var (mestre, jogador, id) = await MesaComHeroiAsync();
        object Edicao(string historia) => new
        {
            personagemId = id, nome = "Thorin Escudo", descricao = "", tipoId = 1,
            personagemPlayer = new { personagemId = id, classe = "Mago", raca = "Anão", nivel = 9, xp = 99999, alinhamento = "Bom" },
            personagemCombate = new { personagemId = id, vidaMaxima = 200, vidaAtual = 150, classeArmadura = 18, proficienciaBonus = 6, dadoVidaMaximo = "9d10", deslocamento = "9m" },
            atributo = new { personagemId = id, forca = 20, destreza = 20, constituicao = 20, inteligencia = 20, sabedoria = 20, carisma = 20 },
            personagemDetalhes = new { personagemId = id, historiaPersonalidade = historia },
        };

        (await jogador.PutAsJsonAsync($"/Personagem/{id}", Edicao("Filho de Thráin."))).EnsureSuccessStatusCode();
        var ficha = await jogador.GetFromJsonAsync<JsonElement>($"/Personagem/{id}");
        Assert.Equal("Thorin Escudo", ficha.GetProperty("nome").GetString());
        Assert.Equal("Bom", ficha.GetProperty("personagemPlayer").GetProperty("alinhamento").GetString());
        Assert.Equal("Filho de Thráin.", ficha.GetProperty("personagemDetalhes").GetProperty("historiaPersonalidade").GetString());
        Assert.Equal(18, ficha.GetProperty("personagemCombate").GetProperty("classeArmadura").GetInt32());
        Assert.Equal("Guerreiro", ficha.GetProperty("personagemPlayer").GetProperty("classe").GetString());
        Assert.Equal(1, ficha.GetProperty("personagemPlayer").GetProperty("nivel").GetInt32());
        Assert.Equal(0, ficha.GetProperty("personagemPlayer").GetProperty("xp").GetInt32());
        Assert.Equal(12, ficha.GetProperty("personagemCombate").GetProperty("vidaMaxima").GetInt32());
        Assert.Equal(12, ficha.GetProperty("personagemCombate").GetProperty("vidaAtual").GetInt32());
        Assert.Equal(16, ficha.GetProperty("atributo").GetProperty("forca").GetInt32());

        (await mestre.PutAsJsonAsync($"/Personagem/{id}", Edicao("Corrigido pelo Mestre."))).EnsureSuccessStatusCode();
        ficha = await mestre.GetFromJsonAsync<JsonElement>($"/Personagem/{id}");
        Assert.Equal(9, ficha.GetProperty("personagemPlayer").GetProperty("nivel").GetInt32());
        Assert.Equal(200, ficha.GetProperty("personagemCombate").GetProperty("vidaMaxima").GetInt32());
        Assert.Equal(20, ficha.GetProperty("atributo").GetProperty("forca").GetInt32());
    }
}
