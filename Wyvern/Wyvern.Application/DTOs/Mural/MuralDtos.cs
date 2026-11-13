using System;
using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Mural
{
    public class MuralResumoDto
    {
        public int MuralId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime AtualizadoEm { get; set; }
    }

    public class MuralDto
    {
        public int MuralId { get; set; }
        public int CampanhaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        /// <summary>O documento do quadro (JSON montado pelo front).</summary>
        public string Conteudo { get; set; } = "{}";
        public int Versao { get; set; }
        public DateTime AtualizadoEm { get; set; }

        public static MuralDto De(Domain.Entities.Mural m) => new()
        {
            MuralId = m.MuralId, CampanhaId = m.CampanhaId, Nome = m.Nome, Conteudo = m.Conteudo, Versao = m.Versao, AtualizadoEm = m.AtualizadoEm,
        };
    }

    public class MuralCriarDto
    {
        [Required(ErrorMessage = "Dê um nome ao mural.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome do mural vai até 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;
    }

    public class MuralSalvarDto
    {
        [Required(ErrorMessage = "Dê um nome ao mural.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome do mural vai até 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Conteudo { get; set; } = "{}";

        /// <summary>A versão que o editor abriu; se o mural já estiver em outra, a gravação é recusada (409).</summary>
        public int Versao { get; set; }
    }

    public class MuralSalvoDto
    {
        public int Versao { get; set; }
        public DateTime AtualizadoEm { get; set; }
    }

    public class MuralImagemCriadaDto
    {
        public int ImagemId { get; set; }
    }
}
