using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Preenche o modelo de ficha (Templates/ficha55.pdf) com os dados do personagem.
    /// Os valores são gravados direto no dicionário de cada campo do PDF, sem criar objetos de campo do PDFsharp:
    /// isso dispensa fontes instaladas no servidor (a imagem Docker não tem) e funciona igual no Windows e no Linux.
    /// </summary>
    public class PdfExportService : IPdfExportService
    {
        public PdfExportService()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public byte[] ExportPdf(Personagem p, string templatePath)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template PDF não encontrado em: {templatePath}");

            var ficha = FichaPdfMapper.Mapear(p);

            using var doc = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify);
            var acroForm = doc.Internals.Catalog.Elements.GetDictionary("/AcroForm")
                ?? throw new InvalidOperationException("O template não possui campos de formulário (AcroForm).");

            var campos = ColetarCampos(acroForm);

            foreach (var (nome, valor) in ficha.Textos)
            {
                if (campos.TryGetValue(FichaPdfMapper.Normalizar(nome), out var campo))
                    DefinirTexto(campo, valor);
            }

            foreach (var (nome, marcada) in ficha.Caixas)
            {
                if (campos.TryGetValue(FichaPdfMapper.Normalizar(nome), out var campo))
                    DefinirCaixa(campo, marcada);
            }

            // Pede ao leitor de PDF que desenhe os campos a partir dos valores (cobre os que não têm aparência pronta).
            acroForm.Elements["/NeedAppearances"] = new PdfBoolean(true);

            using var ms = new MemoryStream();
            doc.Save(ms, false);
            return ms.ToArray();
        }

        // Percorre a árvore de campos e indexa cada um pelo nome sem acentos e com espaços normalizados
        // (o modelo tem nomes como "Race " e "Wpn3 AtkBonus  " com espaços sobrando).
        private static Dictionary<string, PdfDictionary> ColetarCampos(PdfDictionary acroForm)
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

        // Mesma regra do mapeador: minúsculas/sem acento, e espaços repetidos viram um só.
        private static string NormalizarNome(string nome) =>
            FichaPdfMapper.Normalizar(string.Join(' ', nome.Split(' ', StringSplitOptions.RemoveEmptyEntries)));

        private static void DefinirTexto(PdfDictionary campo, string valor)
        {
            // Unicode (UTF-16): preserva acentos e qualquer símbolo, em vez do conjunto limitado do PDFDocEncoding.
            campo.Elements["/V"] = new PdfString(valor, PdfStringEncoding.Unicode);
            // Remove a aparência antiga para o leitor redesenhar o campo com o novo valor.
            campo.Elements.Remove("/AP");
        }

        private static void DefinirCaixa(PdfDictionary campo, bool marcada)
        {
            var estadoLigado = NomeDoEstadoLigado(campo);
            var valor = marcada ? estadoLigado : "/Off";

            campo.Elements["/V"] = new PdfName(valor);
            campo.Elements["/AS"] = new PdfName(valor);
        }

        // O estado "ligado" de uma caixa tem nome próprio no modelo (/Yes, /On...): é a chave de /AP /N que não é /Off.
        private static string NomeDoEstadoLigado(PdfDictionary campo)
        {
            var normal = campo.Elements.GetDictionary("/AP")?.Elements.GetDictionary("/N");
            var ligado = normal?.Elements.Keys.FirstOrDefault(k => k != "/Off");
            return string.IsNullOrEmpty(ligado) ? "/Yes" : ligado;
        }
    }
}
