using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Usuario
{
    /// <summary>O que vai acontecer se a pessoa excluir a conta agora (a tela mostra antes de pedir a senha).</summary>
    public class PreviaDeExclusaoDto
    {
        /// <summary>Campanhas de que é dona e que passam para um co-Mestre.</summary>
        public List<CampanhaTransferidaDto> Transferidas { get; set; } = new();
        /// <summary>Campanhas de que é dona, sem outro Mestre: são encerradas para todos.</summary>
        public List<string> Encerradas { get; set; } = new();
    }

    public class CampanhaTransferidaDto
    {
        public string Campanha { get; set; } = string.Empty;
        public string NovoDono { get; set; } = string.Empty;
    }

    public class ExcluirContaDto
    {
        public const string PalavraDeConfirmacao = "EXCLUIR";

        [Required(ErrorMessage = "Informe a sua senha")]
        public string Senha { get; set; } = string.Empty;

        /// <summary>Precisa ser "EXCLUIR": evita exclusão por clique acidental.</summary>
        [Required(ErrorMessage = "Digite EXCLUIR para confirmar")]
        public string Confirmacao { get; set; } = string.Empty;
    }
}
