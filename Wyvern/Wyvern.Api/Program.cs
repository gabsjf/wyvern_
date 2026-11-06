using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using Wyvern.Api.Extensions;
using Wyvern.Application.Mappings;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Data;
using Wyvern.Infrastructure.Repositories;
using Wyvern.Infrastructure.Repositories.Campanha;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});
builder.Services.AddScoped<IPdfParserService, PdfParserService>();
builder.Services.AddScoped<IPdfExportService, PdfExportService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISessaoValidator, SessaoValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddEnvioDeEmail(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddScoped<ICampanhaAuthorizationService, CampanhaAuthorizationService>();
builder.Services.AddScoped<ICampanhaService, CampanhaService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICombateService, CombateService>();
builder.Services.AddScoped<ICronicaService, CronicaService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Falha na inicialização se Jwt:Key não estiver configurada (não existe chave padrão).
var jwt = JwtSettings.FromConfiguration(builder.Configuration);
var key = Encoding.UTF8.GetBytes(jwt.Key);
const string TokenNaUrl = "wyvern.token-na-url"; // chave em HttpContext.Items: o token desta requisição veio de ?access_token=

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.MapInboundClaims = false; // Prevents claim mapping in newer .NET versions
    x.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        RoleClaimType = "role"
    };

    // Ordem de onde o token vem: cabeçalho Authorization (app nativo, testes), ?access_token= nos hubs (o WebSocket
    // do app nativo não manda cabeçalho) e, na web, o cookie HttpOnly.
    // Na URL só vale o ticket de hub (curto, ver AuthService.GerarTicketHub), nunca o JWT da sessão; e o ticket só vale nos hubs.
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Headers.ContainsKey("Authorization"))
                return Task.CompletedTask;

            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && IsHubPath(context.HttpContext.Request.Path))
            {
                context.Token = accessToken;
                context.HttpContext.Items[TokenNaUrl] = true;
            }
            else if (context.Request.Cookies.TryGetValue(SessionCookie.Nome, out var cookie) && !string.IsNullOrEmpty(cookie))
                context.Token = cookie;

            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var ehTicket = context.Principal?.HasClaim(c => c.Type == AuthService.ClaimTicketHub) == true;
            var naUrl = context.HttpContext.Items.ContainsKey(TokenNaUrl);

            if (naUrl && !ehTicket)
            {
                context.Fail("O token da sessão não é aceito na URL; use um ticket de hub.");
                return;
            }
            if (ehTicket && !IsHubPath(context.HttpContext.Request.Path))
            {
                context.Fail("O ticket de hub só vale nos hubs.");
                return;
            }

            // O JWT em si não é revogável: a sessão por trás dele (família do refresh token, claim "sid") é conferida a cada
            // requisição. Logout, reuso detectado ou usuário desativado derrubam o access token na hora, não só quando ele vence.
            // Token sem "sid" (emitido antes desta checagem existir) não é aceito: não haveria como revogá-lo.
            var sid = context.Principal?.FindFirst(AuthService.SidClaim)?.Value;
            var validador = context.HttpContext.RequestServices.GetRequiredService<ISessaoValidator>();
            if (!Guid.TryParse(sid, out var sessaoId) || !await validador.EstaAtivaAsync(sessaoId))
                context.Fail("Sessão encerrada.");
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CatalogoAdmin", policy => policy.RequireClaim("admin", "true"));
});

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(AtributoProfile).Assembly));
builder.Services.AddSignalR();
builder.Services.AddSingleton<Wyvern.Application.Services.HistoricoRolagens>();

// Atrás de proxy (Render, nginx) o IP real vem em X-Forwarded-For; sem isso o rate limit contaria o proxy inteiro.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// HSTS só vale em produção (o middleware ignora localhost). O padrão é um ano; sem preload, que é difícil de desfazer.
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(builder.Configuration.GetValue("SecurityHeaders:Hsts:MaxAgeDays", 365));
    options.IncludeSubDomains = true;
});

var authPermitLimit =builder.Configuration.GetValue("RateLimit:AuthPermitLimit", 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Por padrão, 10 tentativas por minuto por IP em login, cadastro e entrada por convite.
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermitLimit, Window = TimeSpan.FromMinutes(1) }));

    // Importação de PDF é cara: 5 por minuto por usuário (ou IP, se anônimo).
    options.AddPolicy("upload", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst("nameid")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});

// Origens permitidas vêm da configuração (Cors:AllowedOrigins); sem entradas, nenhuma origem externa é aceita.
// Com cookies a lista é exata (nada de "*"): entrada inválida derruba a inicialização (ver CorsConfiguracao).
var allowedOrigins = CorsConfiguracao.LerOrigens(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
              // Os cabeçalhos que o front manda (X-Requested-With, X-Wyvern-Client, Authorization e os do SignalR) formam uma
              // lista que muda com o cliente; o que protege é a origem exata e o X-Requested-With, conferido no servidor.
              .AllowAnyHeader()
              .AllowCredentials() // cookie de sessão e SignalR
              // O X-Requested-With obriga um preflight em toda mutação; o navegador guarda a resposta por este tempo.
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// PostgreSQL é o banco de produção. SQLite só existe para desenvolvimento local e testes e precisa ser
// pedido explicitamente (Database:Provider = Sqlite), para nunca ser escolhido por engano em produção.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection (variável ConnectionStrings__DefaultConnection).");
var usarSqlite = string.Equals(builder.Configuration["Database:Provider"], "Sqlite", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<WyvernDbContext>(options =>
{
    if (usarSqlite)
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});
builder.Services.AddScoped<CampanhaRepository>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WyvernDbContext>();
    await DatabaseInitializer.InitializeAsync(db, app.Logger);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Wyvern API")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseForwardedHeaders();
app.UseCabecalhosDeSeguranca();
app.ConfigureExceptionHandler(incluirDetalhes: app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseMiddleware<ProtecaoCsrfMiddleware>();

app.UseAuthentication();
app.UseRateLimiter(); // depois da autenticação: a política "upload" particiona por usuário
app.UseAuthorization();

app.MapControllers();
app.MapHub<Wyvern.Api.Hubs.CombatHub>("/combathub");
app.MapHub<Wyvern.Api.Hubs.MesaHub>("/mesahub");
app.MapHub<Wyvern.Api.Hubs.CenaHub>("/cenahub");
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

static bool IsHubPath(PathString path) =>
    path.StartsWithSegments("/combathub") || path.StartsWithSegments("/mesahub") || path.StartsWithSegments("/cenahub");

// Necessário para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }

[JsonSerializable(typeof(IEnumerable<Usuario>))]
[JsonSerializable(typeof(Usuario))]
[JsonSerializable(typeof(Wyvern.Application.DTOs.Auth.LoginDto))]
[JsonSerializable(typeof(Wyvern.Application.DTOs.Auth.RegisterDto))]
[JsonSerializable(typeof(Wyvern.Application.DTOs.Auth.AuthResponseDto))]
[JsonSerializable(typeof(IEnumerable<Wyvern.Application.DTOs.Personagem.PersonagemResponseDto>))]
[JsonSerializable(typeof(Wyvern.Application.DTOs.Personagem.PersonagemResponseDto))]
[JsonSerializable(typeof(IEnumerable<Wyvern.Application.DTOs.Campanha.CampanhaResponseDto>))]
[JsonSerializable(typeof(Wyvern.Application.DTOs.Campanha.CampanhaResponseDto))]
[JsonSerializable(typeof(IEnumerable<Wyvern.Application.DTOs.Sessao.SessaoResponseDto>))]
[JsonSerializable(typeof(Wyvern.Application.DTOs.Sessao.SessaoResponseDto))]
[JsonSerializable(typeof(Wyvern.Domain.Entities.Combate))]
[JsonSerializable(typeof(IEnumerable<Wyvern.Domain.Entities.Combate>))]
[JsonSerializable(typeof(Wyvern.Domain.Entities.CombateParticipante))]
[JsonSerializable(typeof(IEnumerable<Wyvern.Domain.Entities.CombateParticipante>))]
internal partial class AppJsonContext : JsonSerializerContext
{
}