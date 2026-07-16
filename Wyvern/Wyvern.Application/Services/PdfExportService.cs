using System;
using System.IO;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.AcroForms;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
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

            using var doc = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify);
            if (doc.AcroForm == null) throw new Exception("O template não possui campos AcroForm");
            
            var fields = doc.AcroForm.Fields;

            if (doc.AcroForm.Elements.ContainsKey("/NeedAppearances"))
                doc.AcroForm.Elements["/NeedAppearances"] = new PdfSharp.Pdf.PdfBoolean(true);
            else
                doc.AcroForm.Elements.Add("/NeedAppearances", new PdfSharp.Pdf.PdfBoolean(true));

            void SetField(string name, string value)
            {
                if (fields.Names.Contains(name) && fields[name] is PdfTextField txt)
                {
                    txt.Value = new PdfSharp.Pdf.PdfString(value ?? "");
                }
            }

            // 5.5e Mapping (Reverso)
            SetField("Esquerda Centro Single 12", p.Nome);
            
            if (p.PersonagemPlayer != null)
            {
                SetField("Esquerda Centro Single 12_3", p.PersonagemPlayer.Classe);
                SetField("Esquerda Centro Single 12_5", p.PersonagemPlayer.Raca);
                SetField("Esquerda Centro Single 12_2", p.PersonagemPlayer.Antecedente);
                SetField("N#C3#BAmeros Centralizados HV 8 Single_2", p.PersonagemPlayer.Nivel.ToString());
                SetField("Caixa de texto 8", p.PersonagemPlayer.Alinhamento);
                SetField("Esquerda Centro Single 12_4", p.PersonagemPlayer.Subclasse);
                SetField("N#C3#BAmeros Centralizados HV 10 Single", p.PersonagemPlayer.Tamanho);
                SetField("N#C3#BAmeros Centralizados HV 8 Single_3", p.PersonagemPlayer.Xp.ToString());
            }

            if (p.PersonagemDetalhes != null)
            {
                SetField("Multi esquerda 10", p.PersonagemDetalhes.CaracteristicasClasse);
                
                // Quebrar a string formatada do importador
                var details = p.PersonagemDetalhes.HistoriaPersonalidade ?? "";
                SetField("Multi esquerda 10_3", ExtractSection(details, "Traços de Espécie:"));
                SetField("Multi esquerda 10_4", ExtractSection(details, "Talentos:"));
                SetField("Caixa de texto 6_2", ExtractSection(details, "História:"));
            }

            if (p.Atributo != null)
            {
                SetField("N#C3#BAmeros Centralizados HV 8 Single_15", p.Atributo.Forca.ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single_13", p.Atributo.Destreza.ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single_11", p.Atributo.Constituicao.ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single_5", p.Atributo.Inteligencia.ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single_7", p.Atributo.Sabedoria.ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single_9", p.Atributo.Carisma.ToString());
                
                // Calculando os modificadores para os campos grandes
                SetField("N#C3#BAmeros Centralizados HV 8 Single_14", CalcMod(p.Atributo.Forca).ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single_12", CalcMod(p.Atributo.Destreza).ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single_10", CalcMod(p.Atributo.Constituicao).ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single", CalcMod(p.Atributo.Inteligencia).ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single_6", CalcMod(p.Atributo.Sabedoria).ToString());
                SetField("N#C3#BAmeros Centralizados HV 8 Single_8", CalcMod(p.Atributo.Carisma).ToString());
            }

            if (p.PersonagemCombate != null)
            {
                SetField("N#C3#BAmeros Centralizados HV 8 Single_4", p.PersonagemCombate.ClasseArmadura.ToString());
                SetField("N#C3#BAmeros Centralizados HV 12 Single_2", p.PersonagemCombate.Iniciativa.ToString());
                SetField("N#C3#BAmeros Centralizados HV 12 Single_3", p.PersonagemCombate.Deslocamento.ToString());
                SetField("Esquerda baixo 12_4", p.PersonagemCombate.VidaMaxima.ToString());
                SetField("Esquerda baixo 12", p.PersonagemCombate.VidaAtual.ToString());
                SetField("Esquerda baixo 12_5", p.PersonagemCombate.VidaTemporaria.ToString());
            }

            using var ms = new MemoryStream();
            doc.Save(ms, false);
            return ms.ToArray();
        }

        private int CalcMod(int score) => (score - 10) / 2;

        private string ExtractSection(string text, string sectionName)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith(sectionName))
                    return line.Substring(sectionName.Length).Trim();
            }
            return "";
        }
    }
}
