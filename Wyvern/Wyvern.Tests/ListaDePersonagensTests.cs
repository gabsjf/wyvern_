using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

/// <summary>WYV-31: na lista de personagens, o Mestre vê de qual campanha é cada um e de quem ele é.</summary>
public class ListaDePersonagensTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public ListaDePersonagensTests(WyvernApiFactory factory) => _factory = factory;

    /// <summary>Um usuário com nome escolhido (o NovoUsuarioAsync chama todo mundo de "Teste").</summary>
    private async Task<(HttpClient cliente, WyvernApiFactory.Sessao sessao)> UsuarioAsync(string nome)
    {
        var cliente = _factory.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Wyvern-Client", "native");
        var resposta = await _factory.CadastrarEEntrarAsync(cliente, $"{Guid.NewGuid():N}@teste.com", nome);
        resposta.EnsureSuccessStatusCode();
        var sessao = (await resposta.Content.ReadFromJsonAsync<WyvernApiFactory.Sessao>())!;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessao.Token);
        return (cliente, sessao);
    }

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre, string nome)
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome, sistema = "D&D 5.5e" });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task CriarPersonagemAsync(HttpClient dono, int campanhaId, string nome, int tipoId)
    {
        var resposta = await dono.PostAsJsonAsync("/Personagem", new
        {
            nome, campanhaId, tipoId,
            personagemPlayer = Fichas.Origem, atributo = Fichas.Atributos, personagemNpc = Fichas.BlocoSe(tipoId),
            personagemCombate = new { vidaMaxima = 10, vidaAtual = 10, classeArmadura = 12, condicoes = "", deslocamento = "9m" },
        });
        resposta.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task O_mestre_ve_a_campanha_e_o_dono_de_cada_personagem()
    {
        var (mestre, _) = await UsuarioAsync("Mestra Iara");
        var (jogador, _) = await UsuarioAsync("Bruno");
        var estrada = await CriarCampanhaAsync(mestre, "A Estrada Perdida");
        var torre = await CriarCampanhaAsync(mestre, "Torre de Marfim");

        var convite = await (await mestre.PostAsync($"/Campanha/{estrada}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();

        await CriarPersonagemAsync(jogador, estrada, "Thorin", tipoId: 1);
        await CriarPersonagemAsync(mestre, estrada, "Goblin", tipoId: 3);
        await CriarPersonagemAsync(mestre, torre, "Vilã Sombria", tipoId: 2);

        var lista = (await mestre.GetFromJsonAsync<JsonElement>("/Personagem")).EnumerateArray().ToList();
        JsonElement De(string nome) => lista.Single(p => p.GetProperty("nome").GetString() == nome);

        Assert.Equal("A Estrada Perdida", De("Thorin").GetProperty("campanhaNome").GetString());
        Assert.Equal("Bruno", De("Thorin").GetProperty("criadoPorNome").GetString());
        Assert.Equal("A Estrada Perdida", De("Goblin").GetProperty("campanhaNome").GetString());
        Assert.Equal("Mestra Iara", De("Goblin").GetProperty("criadoPorNome").GetString());
        Assert.Equal("Torre de Marfim", De("Vilã Sombria").GetProperty("campanhaNome").GetString());

        // o jogador continua vendo só o que é dele, agora com o nome da campanha
        var doJogador = (await jogador.GetFromJsonAsync<JsonElement>("/Personagem")).EnumerateArray().ToList();
        var thorin = Assert.Single(doJogador);
        Assert.Equal("A Estrada Perdida", thorin.GetProperty("campanhaNome").GetString());
    }
}
