using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wyvern.Application.Services;

namespace Wyvern.Tests;

/// <summary>
/// Tela "Meu perfil": nome, foto, troca de senha (derruba as outras sessões), troca de e-mail (só vale depois do clique no
/// link enviado ao endereço novo) e a lista de aparelhos conectados.
/// </summary>
public class PerfilTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public PerfilTests(WyvernApiFactory factory) => _factory = factory;

    private record Sessao(int UsuarioId, string Token, string RefreshToken);
    private record Conta(bool Beta);
    private record Perfil(int UsuarioId, string Nome, string Email, string? EmailPendente, int FotoVersao, Conta Conta);
    private record SessaoAtiva(Guid Id, string Dispositivo, bool Atual);

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 1, 2, 3 };

    private static string NovoEmail() => $"{Guid.NewGuid():N}@teste.com";

    private HttpClient ClienteNativo(string? token = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Wyvern-Client", "native");
        if (token != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Sessao> CadastrarAsync(string email, string senha = "senha123")
    {
        var resposta = await _factory.CadastrarEEntrarAsync(ClienteNativo(), email, "Aventureira", senha);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<Sessao>(Web))!;
    }

    private async Task<HttpResponseMessage> EntrarAsync(string email, string senha = "senha123") =>
        await ClienteNativo().PostAsJsonAsync("/Auth/login", new { email, senha });

    private async Task<Sessao> OutraSessaoAsync(string email, string senha = "senha123")
    {
        var resposta = await EntrarAsync(email, senha);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<Sessao>(Web))!;
    }

    private async Task<Perfil> PerfilAsync(string token) =>
        (await ClienteNativo(token).GetFromJsonAsync<Perfil>("/Usuario/me", Web))!;

    private async Task<HttpStatusCode> MeAsync(string token) =>
        (await ClienteNativo(token).GetAsync("/Auth/me")).StatusCode;

    private static MultipartFormDataContent Foto(byte[] bytes)
    {
        var form = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(bytes);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(arquivo, "Foto", "foto.png");
        return form;
    }

    private string TokenDeTrocaDeEmail(string novoEmail)
    {
        var corpo = _factory.Emails.Para(novoEmail).Last(e => e.Assunto.StartsWith("Confirme o novo")).Corpo;
        return Uri.UnescapeDataString(Regex.Match(corpo, @"token=(\S+)").Groups[1].Value);
    }

    // ---------- nome ----------

    [Fact]
    public async Task Perfil_mostra_a_conta_e_o_nome_trocado_aparece_tambem_no_auth_me()
    {
        var email = NovoEmail();
        var sessao = await CadastrarAsync(email);

        var perfil = await PerfilAsync(sessao.Token);
        Assert.Equal(email, perfil.Email);
        Assert.Equal(0, perfil.FotoVersao);
        Assert.True(perfil.Conta.Beta);

        var troca = await ClienteNativo(sessao.Token).PutAsJsonAsync("/Usuario/me/nome", new { nome = "  Joana Tempestade  " });
        Assert.Equal(HttpStatusCode.OK, troca.StatusCode);
        Assert.Equal("Joana Tempestade", (await PerfilAsync(sessao.Token)).Nome);

        // o token ainda carrega o nome antigo, mas /Auth/me lê do banco
        var me = await ClienteNativo(sessao.Token).GetFromJsonAsync<JsonElement>("/Auth/me");
        Assert.Equal("Joana Tempestade", me.GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Nome_vazio_e_recusado()
    {
        var sessao = await CadastrarAsync(NovoEmail());
        var resposta = await ClienteNativo(sessao.Token).PutAsJsonAsync("/Usuario/me/nome", new { nome = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task O_antigo_put_por_id_que_trocava_o_email_sem_confirmacao_nao_existe_mais()
    {
        var sessao = await CadastrarAsync(NovoEmail());
        var resposta = await ClienteNativo(sessao.Token).PutAsJsonAsync($"/Usuario/{sessao.UsuarioId}", new { nome = "X", email = NovoEmail() });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, resposta.StatusCode);
    }

    // ---------- foto ----------

    [Fact]
    public async Task Foto_enviada_e_servida_com_etag_e_removida()
    {
        var sessao = await CadastrarAsync(NovoEmail());
        var client = ClienteNativo(sessao.Token);

        var envio = await client.PutAsync("/Usuario/me/foto", Foto(Png));
        Assert.Equal(HttpStatusCode.OK, envio.StatusCode);
        Assert.Equal(1, (await PerfilAsync(sessao.Token)).FotoVersao);

        var foto = await client.GetAsync("/Usuario/me/foto");
        Assert.Equal(HttpStatusCode.OK, foto.StatusCode);
        Assert.Equal("image/png", foto.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await foto.Content.ReadAsByteArrayAsync());

        var etag = foto.Headers.ETag!;
        var deNovo = new HttpRequestMessage(HttpMethod.Get, "/Usuario/me/foto");
        deNovo.Headers.IfNoneMatch.Add(etag);
        Assert.Equal(HttpStatusCode.NotModified, (await client.SendAsync(deNovo)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/Usuario/me/foto")).StatusCode);
        Assert.Equal(0, (await PerfilAsync(sessao.Token)).FotoVersao);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Usuario/me/foto")).StatusCode);
    }

    [Fact]
    public async Task Foto_que_nao_e_imagem_ou_grande_demais_e_recusada()
    {
        var sessao = await CadastrarAsync(NovoEmail());
        var client = ClienteNativo(sessao.Token);

        var texto = await client.PutAsync("/Usuario/me/foto", Foto("isto nao e uma imagem"u8.ToArray()));
        Assert.Equal(HttpStatusCode.BadRequest, texto.StatusCode);

        var grande = new byte[2 * 1024 * 1024 + 1];
        Png.CopyTo(grande, 0);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync("/Usuario/me/foto", Foto(grande))).StatusCode);

        Assert.Equal(0, (await PerfilAsync(sessao.Token)).FotoVersao);
    }

    [Fact]
    public async Task Foto_de_uma_conta_nao_aparece_para_outra()
    {
        var dona = await CadastrarAsync(NovoEmail());
        var outra = await CadastrarAsync(NovoEmail());
        await ClienteNativo(dona.Token).PutAsync("/Usuario/me/foto", Foto(Png));

        // as rotas são só "me": a outra conta enxerga a própria (que não existe)
        Assert.Equal(HttpStatusCode.NotFound, (await ClienteNativo(outra.Token).GetAsync("/Usuario/me/foto")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.NovoClienteWeb().GetAsync("/Usuario/me/foto")).StatusCode);
    }

    // ---------- senha ----------

    [Fact]
    public async Task Senha_atual_errada_e_recusada()
    {
        var sessao = await CadastrarAsync(NovoEmail());
        var resposta = await ClienteNativo(sessao.Token).PostAsJsonAsync("/Usuario/me/senha", new { senhaAtual = "errada", novaSenha = "nova-senha-1" });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Trocar_a_senha_mantem_esta_sessao_e_derruba_as_outras()
    {
        var email = NovoEmail();
        var esta = await CadastrarAsync(email);
        var outra = await OutraSessaoAsync(email);
        Assert.Equal(HttpStatusCode.OK, await MeAsync(outra.Token));

        var troca = await ClienteNativo(esta.Token).PostAsJsonAsync("/Usuario/me/senha", new { senhaAtual = "senha123", novaSenha = "nova-senha-1" });
        Assert.Equal(HttpStatusCode.NoContent, troca.StatusCode);

        Assert.Equal(HttpStatusCode.OK, await MeAsync(esta.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(outra.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await ClienteNativo().PostAsJsonAsync("/Auth/refresh", new { outra.RefreshToken })).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await EntrarAsync(email, "senha123")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(email, "nova-senha-1")).StatusCode);
    }

    [Fact]
    public async Task Trocar_a_senha_avisa_por_email_com_o_caminho_para_reagir()
    {
        var email = NovoEmail();
        var sessao = await CadastrarAsync(email);

        (await ClienteNativo(sessao.Token).PostAsJsonAsync("/Usuario/me/senha", new { senhaAtual = "senha123", novaSenha = "nova-senha-1" })).EnsureSuccessStatusCode();

        var aviso = _factory.Emails.Para(email).Last();
        Assert.StartsWith("A senha da sua conta foi alterada", aviso.Assunto);
        Assert.Contains("/esqueci-senha", aviso.Corpo);
        Assert.DoesNotContain("nova-senha-1", aviso.Corpo);
    }

    [Fact]
    public async Task Nova_senha_igual_a_atual_e_recusada()
    {
        var sessao = await CadastrarAsync(NovoEmail());

        var resposta = await ClienteNativo(sessao.Token).PostAsJsonAsync("/Usuario/me/senha", new { senhaAtual = "senha123", novaSenha = "senha123" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("diferente da atual", JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement.ToString());
    }

    [Fact]
    public async Task Trocar_a_senha_invalida_um_link_de_redefinicao_pedido_antes()
    {
        var email = NovoEmail();
        var sessao = await CadastrarAsync(email);
        (await ClienteNativo().PostAsJsonAsync("/Auth/esqueci-senha", new { email })).EnsureSuccessStatusCode();
        var link = _factory.TokenDeRedefinicao(email);

        (await ClienteNativo(sessao.Token).PostAsJsonAsync("/Usuario/me/senha", new { senhaAtual = "senha123", novaSenha = "nova-senha-1" })).EnsureSuccessStatusCode();

        var redefinir = await ClienteNativo().PostAsJsonAsync("/Auth/redefinir-senha", new { token = link, novaSenha = "outra-senha-2" });
        Assert.Equal(HttpStatusCode.BadRequest, redefinir.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(email, "nova-senha-1")).StatusCode);
    }

    // ---------- e-mail ----------

    [Fact]
    public async Task Troca_de_email_so_vale_depois_do_clique_e_avisa_o_endereco_antigo()
    {
        var antigo = NovoEmail();
        var novo = NovoEmail();
        var sessao = await CadastrarAsync(antigo);
        var client = ClienteNativo(sessao.Token);

        var pedido = await client.PostAsJsonAsync("/Usuario/me/email", new { novoEmail = novo, senhaAtual = "senha123" });
        Assert.Equal(HttpStatusCode.Accepted, pedido.StatusCode);
        Assert.Equal(novo, (await PerfilAsync(sessao.Token)).EmailPendente);

        // antes do clique, nada mudou
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(antigo)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await EntrarAsync(novo)).StatusCode);

        var token = TokenDeTrocaDeEmail(novo);
        var confirmacao = await _factory.NovoClienteWeb().PostAsJsonAsync("/Auth/confirmar-troca-email", new { token });
        Assert.Equal(HttpStatusCode.NoContent, confirmacao.StatusCode);

        var perfil = await PerfilAsync(sessao.Token);
        Assert.Equal(novo, perfil.Email);
        Assert.Null(perfil.EmailPendente);
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(novo)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await EntrarAsync(antigo)).StatusCode);

        var aviso = _factory.Emails.Para(antigo).Last();
        Assert.StartsWith("O e-mail da sua conta foi alterado", aviso.Assunto);
        Assert.DoesNotContain(novo, aviso.Corpo); // vai mascarado

        // o link é de uso único
        var reuso = await _factory.NovoClienteWeb().PostAsJsonAsync("/Auth/confirmar-troca-email", new { token });
        Assert.Equal(HttpStatusCode.BadRequest, reuso.StatusCode);
    }

    [Fact]
    public async Task Troca_de_email_com_senha_errada_nao_manda_nada()
    {
        var sessao = await CadastrarAsync(NovoEmail());
        var novo = NovoEmail();

        var pedido = await ClienteNativo(sessao.Token).PostAsJsonAsync("/Usuario/me/email", new { novoEmail = novo, senhaAtual = "errada" });

        Assert.Equal(HttpStatusCode.BadRequest, pedido.StatusCode);
        Assert.Empty(_factory.Emails.Para(novo));
        Assert.Null((await PerfilAsync(sessao.Token)).EmailPendente);
    }

    [Fact]
    public async Task Email_de_outra_conta_recebe_a_mesma_resposta_mas_nenhum_link()
    {
        var ocupado = NovoEmail();
        await CadastrarAsync(ocupado);
        var sessao = await CadastrarAsync(NovoEmail());
        var antes = _factory.Emails.Para(ocupado).Count();

        var pedido = await ClienteNativo(sessao.Token).PostAsJsonAsync("/Usuario/me/email", new { novoEmail = ocupado, senhaAtual = "senha123" });

        Assert.Equal(HttpStatusCode.Accepted, pedido.StatusCode);
        Assert.Equal(antes, _factory.Emails.Para(ocupado).Count());
    }

    [Fact]
    public async Task Email_que_ficou_ocupado_antes_do_clique_e_recusado()
    {
        var antigo = NovoEmail();
        var novo = NovoEmail();
        var sessao = await CadastrarAsync(antigo);
        await ClienteNativo(sessao.Token).PostAsJsonAsync("/Usuario/me/email", new { novoEmail = novo, senhaAtual = "senha123" });
        var token = TokenDeTrocaDeEmail(novo);

        // outra pessoa cadastra o endereço antes do clique
        await _factory.NovoClienteWeb().PostAsJsonAsync("/Auth/register", new { nome = "Outra", email = novo, senha = "senha123" });

        var confirmacao = await _factory.NovoClienteWeb().PostAsJsonAsync("/Auth/confirmar-troca-email", new { token });
        Assert.Equal(HttpStatusCode.BadRequest, confirmacao.StatusCode);
        Assert.Equal(antigo, (await PerfilAsync(sessao.Token)).Email);
    }

    [Fact]
    public async Task Trocar_para_o_proprio_email_e_recusado()
    {
        var email = NovoEmail();
        var sessao = await CadastrarAsync(email);
        var pedido = await ClienteNativo(sessao.Token).PostAsJsonAsync("/Usuario/me/email", new { novoEmail = email, senhaAtual = "senha123" });
        Assert.Equal(HttpStatusCode.BadRequest, pedido.StatusCode);
    }

    // ---------- aparelhos conectados ----------

    [Fact]
    public async Task Lista_os_aparelhos_e_encerra_um_deles()
    {
        var email = NovoEmail();
        var esta = await CadastrarAsync(email);
        var outra = await OutraSessaoAsync(email);
        var client = ClienteNativo(esta.Token);

        var sessoes = (await client.GetFromJsonAsync<List<SessaoAtiva>>("/Usuario/me/sessoes", Web))!;
        Assert.Equal(2, sessoes.Count);
        Assert.Single(sessoes, s => s.Atual);
        Assert.All(sessoes, s => Assert.StartsWith("App Wyvern", s.Dispositivo));

        var atual = sessoes.Single(s => s.Atual).Id;
        var alheia = sessoes.Single(s => !s.Atual).Id;

        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/Usuario/me/sessoes/{atual}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/Usuario/me/sessoes/{alheia}")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(outra.Token));
        Assert.Equal(HttpStatusCode.OK, await MeAsync(esta.Token));
        Assert.Single((await client.GetFromJsonAsync<List<SessaoAtiva>>("/Usuario/me/sessoes", Web))!);
    }

    [Fact]
    public async Task Nao_encerra_sessao_de_outra_pessoa()
    {
        var vitima = await CadastrarAsync(NovoEmail());
        var intrusa = await CadastrarAsync(NovoEmail());
        var daVitima = (await ClienteNativo(vitima.Token).GetFromJsonAsync<List<SessaoAtiva>>("/Usuario/me/sessoes", Web))!.Single().Id;

        var resposta = await ClienteNativo(intrusa.Token).DeleteAsync($"/Usuario/me/sessoes/{daVitima}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, await MeAsync(vitima.Token));
    }

    [Fact]
    public async Task Encerrar_as_outras_mantem_so_esta()
    {
        var email = NovoEmail();
        var esta = await CadastrarAsync(email);
        var segunda = await OutraSessaoAsync(email);
        var terceira = await OutraSessaoAsync(email);

        Assert.Equal(HttpStatusCode.NoContent, (await ClienteNativo(esta.Token).DeleteAsync("/Usuario/me/sessoes")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, await MeAsync(esta.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(segunda.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(terceira.Token));
    }

    // ---------- rótulo do aparelho ----------

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36", false, "Chrome em Windows")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0", false, "Edge em Windows")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1", false, "Safari em iOS")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36", false, "Chrome em Android")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 14.0; rv:132.0) Gecko/20100101 Firefox/132.0", false, "Firefox em macOS")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8; wv) AppleWebKit/537.36", true, "App Wyvern (Android)")]
    [InlineData("", false, "Aparelho desconhecido")]
    public void Descreve_o_aparelho_pelo_user_agent(string userAgent, bool appNativo, string esperado) =>
        Assert.Equal(esperado, DescritorDeDispositivo.Descrever(userAgent, appNativo));
}
