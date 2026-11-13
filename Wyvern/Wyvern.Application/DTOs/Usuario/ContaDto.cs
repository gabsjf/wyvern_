using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Usuario
{
    /// <summary>Tipo de conta, plano e limites de quem está logado (cartão "Plano" do perfil).</summary>
    public class ContaDto
    {
        /// <summary>Chave do plano ("Free", "Standard"...).</summary>
        public string Plano { get; set; } = "Free";
        public string NomePlano { get; set; } = "Free";
        /// <summary>Durante o beta toda conta tem acesso completo, sem limites.</summary>
        public bool Beta { get; set; }
        public bool PodeCriarCampanha { get; set; }
        /// <summary>Por que não pode criar campanha agora; nulo quando pode.</summary>
        public string? MotivoBloqueio { get; set; }
        /// <summary>Nulo = sem limite.</summary>
        public int? MaxCampanhas { get; set; }
        /// <summary>Jogadores por campanha além do Mestre dono. Nulo = sem limite.</summary>
        public int? MaxJogadoresPorCampanha { get; set; }
        /// <summary>Campanhas ativas de que a pessoa é dona.</summary>
        public int CampanhasCriadas { get; set; }
    }

    public class PlanoPublicoDto
    {
        public string Chave { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public int? MaxCampanhas { get; set; }
        public int? MaxJogadoresPorCampanha { get; set; }
    }

    public class CatalogoDePlanosDto
    {
        public bool Beta { get; set; }
        public List<PlanoPublicoDto> Planos { get; set; } = new();
    }

    public class DefinirPlanoDto
    {
        [Required(ErrorMessage = "Informe o plano")]
        public string Plano { get; set; } = string.Empty;
    }
}
