using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Acesso aos campos do formulário (AcroForm) direto pelos dicionários do PDF, usado por exportar e importar.
    /// Os campos são indexados pelo nome sem acentos, em minúsculas e com espaços repetidos reduzidos a um
    /// (o modelo tem nomes como "Race " e "Wpn3 AtkBonus  " com espaços sobrando).
    /// </summary>
    internal static class PdfCampos
    {
        public static Dictionary<string, PdfDictionary> Coletar(PdfDictionary acroForm)
        {
            var resultado = new Dictionary<string, PdfDictionary>(StringComparer.Ordinal);
            var raiz = acroForm.Elements.GetArray("/Fields");
            if (raiz != null) Percorrer(raiz, prefixo: "", resultado);
            return resultado;
        }

        private static void Percorrer(PdfArray itens, string prefixo, Dictionary<string, PdfDictionary> destino)
        {
            foreach (var item in itens.Elements)
            {
                var campo = (item as PdfReference)?.Value as PdfDictionary ?? item as PdfDictionary;
                if (campo == null) continue;

                var parcial = campo.Elements.GetString("/T");
                var nome = string.IsNullOrEmpty(prefixo) ? parcial : string.IsNullOrEmpty(parcial) ? prefixo : prefixo + "." + parcial;

                var filhos = campo.Elements.GetArray("/Kids");
                var filhosSaoCampos = filhos != null && filhos.Elements.Any(k =>
                    ((k as PdfReference)?.Value as PdfDictionary)?.Elements.ContainsKey("/T") == true);

                if (filhosSaoCampos)
                {
                    Percorrer(filhos!, nome, destino);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(nome))
                    destino.TryAdd(NormalizarNome(nome), campo);
            }
        }

        public static string NormalizarNome(string nome) =>
            FichaPdfMapper.Normalizar(string.Join(' ', nome.Split(' ', StringSplitOptions.RemoveEmptyEntries)));

        /// <summary>Valor de texto do campo ("" se vazio ou se não for texto).</summary>
        public static string Texto(PdfDictionary campo) => campo.Elements["/V"] switch
        {
            PdfString s => s.Value ?? "",
            PdfName n => n.Value.TrimStart('/'),
            _ => "",
        };

        /// <summary>Caixa marcada: valor diferente de vazio, /Off e "Off".</summary>
        public static bool Marcada(PdfDictionary campo)
        {
            var valor = campo.Elements["/V"] is PdfName n ? n.Value : campo.Elements["/AS"] is PdfName a ? a.Value : Texto(campo);
            valor = valor.TrimStart('/');
            return valor != "" && !valor.Equals("Off", StringComparison.OrdinalIgnoreCase) && valor != "0";
        }
    }
}
