namespace Wyvern.Application.DTOs.Auth
{
    /// <summary>Corpo de /Auth/refresh e /Auth/logout do app nativo; na web o refresh token vem no cookie e não há corpo.</summary>
    public class RefreshDto
    {
        public string? RefreshToken { get; set; }
    }
}
