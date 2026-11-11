using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wyvern.Api.Controllers;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

/// <summary>Murais do Mestre: só ele vê e edita, o documento é salvo com controle de versão e as imagens ficam à parte.</summary>
public class MuraisTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public MuraisTests(WyvernApiFactory factory) => _factory = factory;

    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var r = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5e" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task EntrarAsync(HttpClient mestre, HttpClient jogador, int campanhaId)
    {
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> CriarMuralAsync(HttpClient mestre, int campanhaId, string nome = "Intrigas de Vael")
    {
        var r = await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Murais", new { nome });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<HttpResponseMessage> SalvarAsync(HttpClient mestre, int campanhaId, int muralId, string conteudo, int versao, string nome = "Intrigas de Vael") =>
        mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Murais/{muralId}", new { nome, conteudo, versao });

    private static async Task<int> EnviarImagemAsync(HttpClient mestre, int campanhaId, int muralId, byte[]? bytes = null)
    {
        using var form = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(bytes ?? Png);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(arquivo, "Imagem", "pista.png");
        var r = await mestre.PostAsync($"/Campanha/{campanhaId}/Murais/{muralId}/imagens", form);
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imagemId").GetInt32();
    }

    [Fact]
    public async Task Mestre_cria_salva_reabre_lista_e_exclui()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var criado = await CriarMuralAsync(mestre, campanhaId);
        var muralId = criado.GetProperty("muralId").GetInt32();
        Assert.Equal(1, criado.GetProperty("versao").GetInt32());

        var conteudo = """{"elementos":[{"id":"a","tipo":"nota","x":10,"y":20,"w":200,"h":200,"texto":"O duque mente"}],"conectores":[]}""";
        var salvo = await SalvarAsync(mestre, campanhaId, muralId, conteudo, 1, nome: "Intrigas da corte");
        Assert.Equal(HttpStatusCode.OK, salvo.StatusCode);
        Assert.Equal(2, (await salvo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("versao").GetInt32());

        var aberto = await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Murais/{muralId}");
        Assert.Equal(conteudo, aberto.GetProperty("conteudo").GetString());
        Assert.Equal("Intrigas da corte", aberto.GetProperty("nome").GetString());

        var lista = await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Murais");
        Assert.Equal("Intrigas da corte", Assert.Single(lista.EnumerateArray()).GetProperty("nome").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await mestre.DeleteAsync($"/Campanha/{campanhaId}/Murais/{muralId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await mestre.GetAsync($"/Campanha/{campanhaId}/Murais/{muralId}")).StatusCode);
    }

    [Fact]
    public async Task Jogador_nao_ve_nem_edita_os_murais_do_mestre()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);
        var muralId = (await CriarMuralAsync(mestre, campanhaId)).GetProperty("muralId").GetInt32();

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.GetAsync($"/Campanha/{campanhaId}/Murais")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.GetAsync($"/Campanha/{campanhaId}/Murais/{muralId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SalvarAsync(jogador, campanhaId, muralId, "{}", 1)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PostAsJsonAsync($"/Campanha/{campanhaId}/Murais", new { nome = "Meu" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.DeleteAsync($"/Campanha/{campanhaId}/Murais/{muralId}")).StatusCode);
    }

    [Fact]
    public async Task Quem_nao_participa_da_campanha_recebe_404()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var muralId = (await CriarMuralAsync(mestre, campanhaId)).GetProperty("muralId").GetInt32();

        Assert.Equal(HttpStatusCode.NotFound, (await estranho.GetAsync($"/Campanha/{campanhaId}/Murais/{muralId}")).StatusCode);
    }

    [Fact]
    public async Task Mural_de_outra_campanha_nao_abre_pelo_endereco_errado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var a = await CriarCampanhaAsync(mestre);
        var b = await CriarCampanhaAsync(mestre);
        var muralDeA = (await CriarMuralAsync(mestre, a)).GetProperty("muralId").GetInt32();

        Assert.Equal(HttpStatusCode.NotFound, (await mestre.GetAsync($"/Campanha/{b}/Murais/{muralDeA}")).StatusCode);
    }

    [Fact]
    public async Task Salvar_com_versao_antiga_da_conflito_em_vez_de_apagar_a_outra_janela()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var muralId = (await CriarMuralAsync(mestre, campanhaId)).GetProperty("muralId").GetInt32();
        (await SalvarAsync(mestre, campanhaId, muralId, """{"elementos":[]}""", 1)).EnsureSuccessStatusCode();

        var atrasada = await SalvarAsync(mestre, campanhaId, muralId, """{"elementos":[{"id":"velho"}]}""", 1);

        Assert.Equal(HttpStatusCode.Conflict, atrasada.StatusCode);
        Assert.Equal(2, (await atrasada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("versao").GetInt32());
        var aberto = await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Murais/{muralId}");
        Assert.DoesNotContain("velho", aberto.GetProperty("conteudo").GetString());
    }

    [Theory]
    [InlineData("isto não é json")]
    [InlineData("[1,2,3]")]
    public async Task Conteudo_invalido_e_recusado(string conteudo)
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var muralId = (await CriarMuralAsync(mestre, campanhaId)).GetProperty("muralId").GetInt32();

        Assert.Equal(HttpStatusCode.BadRequest, (await SalvarAsync(mestre, campanhaId, muralId, conteudo, 1)).StatusCode);
    }

    [Fact]
    public async Task Nome_vazio_e_recusado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Murais", new { nome = "" })).StatusCode);
    }

    [Fact]
    public async Task Imagem_enviada_volta_com_cache_e_arquivo_que_nao_e_imagem_e_recusado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var muralId = (await CriarMuralAsync(mestre, campanhaId)).GetProperty("muralId").GetInt32();

        var imagemId = await EnviarImagemAsync(mestre, campanhaId, muralId);
        var resposta = await mestre.GetAsync($"/Campanha/{campanhaId}/Murais/{muralId}/imagens/{imagemId}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("image/png", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("immutable", resposta.Headers.CacheControl?.ToString());
        Assert.Equal(Png, await resposta.Content.ReadAsByteArrayAsync());

        using var form = new MultipartFormDataContent { { new ByteArrayContent("nao sou imagem"u8.ToArray()), "Imagem", "x.png" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsync($"/Campanha/{campanhaId}/Murais/{muralId}/imagens", form)).StatusCode);
    }

    [Fact]
    public async Task Imagem_que_saiu_do_mural_e_apagada_depois_da_carencia_e_a_que_ficou_nao()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var muralId = (await CriarMuralAsync(mestre, campanhaId)).GetProperty("muralId").GetInt32();
        var usada = await EnviarImagemAsync(mestre, campanhaId, muralId);
        var largada = await EnviarImagemAsync(mestre, campanhaId, muralId);
        var recente = await EnviarImagemAsync(mestre, campanhaId, muralId);

        // as duas primeiras foram enviadas "há uma hora"; a terceira acabou de chegar e ainda pode entrar no documento
        using (var escopo = _factory.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
            foreach (var img in db.MuralImagens.Where(i => i.MuralImagemId == usada || i.MuralImagemId == largada))
                img.CriadoEm = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
        }

        (await SalvarAsync(mestre, campanhaId, muralId, $$"""{"elementos":[{"id":"i","tipo":"imagem","imagemId":{{usada}}}]}""", 1)).EnsureSuccessStatusCode();

        string Url(int id) => $"/Campanha/{campanhaId}/Murais/{muralId}/imagens/{id}";
        Assert.Equal(HttpStatusCode.OK, (await mestre.GetAsync(Url(usada))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await mestre.GetAsync(Url(largada))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await mestre.GetAsync(Url(recente))).StatusCode);
    }

    [Fact]
    public async Task Excluir_a_campanha_leva_os_murais_junto()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var muralId = (await CriarMuralAsync(mestre, campanhaId)).GetProperty("muralId").GetInt32();
        await EnviarImagemAsync(mestre, campanhaId, muralId);

        // a exclusão de campanha é lógica (Ativo = false): o mural continua guardado, mas ninguém mais o alcança
        (await mestre.DeleteAsync($"/Campanha/{campanhaId}")).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NotFound, (await mestre.GetAsync($"/Campanha/{campanhaId}/Murais/{muralId}")).StatusCode);
    }

    [Fact]
    public void Acha_as_imagens_usadas_em_qualquer_lugar_do_documento()
    {
        using var doc = JsonDocument.Parse("""
            {"elementos":[{"tipo":"imagem","imagemId":3},{"tipo":"grupo","filhos":[{"imagemId":7}]},{"imagemId":"9"}],"fundo":{"imagemId":12}}
            """);
        Assert.Equal(new[] { 3, 7, 12 }, MuralController.ImagensUsadas(doc.RootElement).OrderBy(x => x));
    }
}
