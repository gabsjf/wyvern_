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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "WyvernSuperSecretKey1234567890!!";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.MapInboundClaims = false; // Prevents claim mapping in newer .NET versions
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        RoleClaimType = "role"
    };
});
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(AtributoProfile).Assembly));
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // allow any origin
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // SignalR needs this
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddDbContext<WyvernDbContext>(options =>
{
    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});
builder.Services.AddScoped<CampanhaRepository>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WyvernDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Wyvern API")
               .WithTheme(ScalarTheme.Moon);
    });
    app.ConfigureExceptionHandler();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<Wyvern.Api.Hubs.CombatHub>("/combathub");


app.Run();

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