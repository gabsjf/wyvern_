using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

/// <summary>
/// O cadastro responde igual com e-mail novo ou já usado (ninguém descobre quem tem conta); o resultado chega por e-mail
/// ao dono, e a conta só entra depois de confirmar o e-mail.
/// </summary>
public class CadastroNeutroTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public CadastroNeutroTests(WyvernApiFactory factory) => _factory = factory;

    private static string NovoEmail() => $"{Guid.NewGuid():N}@teste.com";

    private static Task<HttpResponseMessage> Cadastrar(HttpClient client, string email, string senha = "senha123", string nome = "Neutro") =>
        client.PostAsJsonAsync("/Auth/register", new { nome, email, senha });

    [Fact]
    public async Task Resposta_do_cadastro_e_identica_com_email_novo_e_ja_usado()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);

        var novo = await Cadastrar(client, NovoEmail());
        var repetido = await Cadastrar(client, email);

        Assert.Equal(novo.StatusCode, repetido.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, repetido.StatusCode);
        Assert.Equal(await novo.Content.ReadAsStringAsync(), await repetido.Content.ReadAsStringAsync());
        Assert.False(repetido.Headers.Contains("Set-Cookie")); // cadastro nunca abre sessão
    }

    [Fact]
    public async Task Cadastro_novo_manda_link_de_confirmacao_para_o_email()
    {
        var email = NovoEmail();

        await Cadastrar(_factory.NovoClienteWeb(), email);

        var mensagem = Assert.Single(_factory.Emails.Para(email));
        Assert.StartsWith("Confirme", mensagem.Assunto);
        Assert.Contains("http://localhost:4200/confirmar-email?token=", mensagem.Corpo);
    }

    [Fact]
    public async Task Cadastro_com_email_de_conta_existente_avisa_o_dono_e_nao_altera_a_conta()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);
        var antes = _factory.Emails.Para(email).Count();

        await Cadastrar(client, email, senha: "senha-do-intruso");

        var aviso = _factory.Emails.Para(email).Skip(antes).Single();
        Assert.StartsWith("Alguém tentou", aviso.Assunto);
        Assert.DoesNotContain("token=", aviso.Corpo);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/Auth/login", new { email, senha = "senha-do-intruso" })).StatusCode);
    }

    [Fact]
    public async Task Login_sem_confirmar_o_email_e_barrado_so_para_quem_sabe_a_senha()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await Cadastrar(client, email);

        var certa = await client.PostAsJsonAsync("/Auth/login", new { email, senha = "senha123" });
        var errada = await client.PostAsJsonAsync("/Auth/login", new { email, senha = "errada123" });

        Assert.Equal(HttpStatusCode.Unauthorized, certa.StatusCode);
        Assert.Contains("Confirme", await certa.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, errada.StatusCode);
        Assert.Contains("inválidos", await errada.Content.ReadAsStringAsync()); // quem não tem a senha não aprende nada
    }

    [Fact]
    public async Task Confirmar_o_email_libera_o_login_e_o_link_nao_serve_duas_vezes()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await Cadastrar(client, email);
        var token = _factory.TokenDeConfirmacao(email);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/Auth/confirmar-email", new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/Auth/login", new { email, senha = "senha123" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/Auth/confirmar-email", new { token })).StatusCode);
    }

    [Theory]
    [InlineData("lixo")]
    [InlineData("999999.abc")]
    [InlineData(".abc")]
    public async Task Token_de_confirmacao_invalido_e_recusado(string token)
    {
        var resposta = await _factory.NovoClienteWeb().PostAsJsonAsync("/Auth/confirmar-email", new { token });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Token_de_outro_usuario_ou_adulterado_nao_confirma()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await Cadastrar(client, email);
        var token = _factory.TokenDeConfirmacao(email);

        var adulterado = await client.PostAsJsonAsync("/Auth/confirmar-email", new { token = token + "x" });

        Assert.Equal(HttpStatusCode.BadRequest, adulterado.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/Auth/login", new { email, senha = "senha123" })).StatusCode);
    }

    [Fact]
    public async Task Link_vencido_nao_confirma()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await Cadastrar(client, email);
        var token = _factory.TokenDeConfirmacao(email);
        using (var escopo = _factory.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
            var user = db.Usuarios.Single(u => u.Email == email);
            user.TokenConfirmacaoExpiraEm = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/Auth/confirmar-email", new { token })).StatusCode);
    }

    [Fact]
    public async Task Recadastro_antes_de_confirmar_manda_outro_link_sem_trocar_a_senha()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await Cadastrar(client, email, senha: "senha-do-dono");
        var primeiro = _factory.TokenDeConfirmacao(email);

        await Cadastrar(client, email, senha: "senha-do-intruso");
        var segundo = _factory.TokenDeConfirmacao(email);

        Assert.NotEqual(primeiro, segundo);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/Auth/confirmar-email", new { token = primeiro })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/Auth/confirmar-email", new { token = segundo })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/Auth/login", new { email, senha = "senha-do-dono" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/Auth/login", new { email, senha = "senha-do-intruso" })).StatusCode);
    }
}
