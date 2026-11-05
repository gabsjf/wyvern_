using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace Wyvern.Api.Extensions
{
    /// <summary>
    /// O JWT da web vive num cookie HttpOnly: o JavaScript da página não consegue lê-lo (nem um script injetado).
    /// O app nativo (Capacitor) é de outra origem e continua com Bearer, pedido pelo cabeçalho X-Wyvern-Client.
    /// </summary>
    public static class SessionCookie
    {
        public const string Nome = "wyvern_at";
        public const string CabecalhoCliente = "X-Wyvern-Client";
        public const string CabecalhoCsrf = "X-Requested-With";
        public const string ValorCsrf = "wyvern";

        public static void Gravar(HttpContext context, string token)
        {
            var expira = new DateTimeOffset(new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo, TimeSpan.Zero);
            context.Response.Cookies.Append(Nome, token, Opcoes(context, expira));
        }

        public static void Apagar(HttpContext context) => context.Response.Cookies.Delete(Nome, Opcoes(context, null));

        public static bool ClienteNativo(HttpRequest request) =>
            string.Equals(request.Headers[CabecalhoCliente], "native", StringComparison.OrdinalIgnoreCase);

        // Secure só com HTTPS (atrás do proxy, o UseForwardedHeaders já faz IsHttps valer); em desenvolvimento local é HTTP.
        // Lax basta porque o front (wyvern.sbs) e a API (api.wyvern.sbs) são o mesmo site.
        private static CookieOptions Opcoes(HttpContext context, DateTimeOffset? expira) => new()
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expira,
            IsEssential = true
        };
    }
}
