using System;
using System.IO;
using System.Collections.Generic;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    public class PdfParserService : IPdfParserService
    {
        public PdfParserService()
        {
            // Required for PdfSharp to decode text properly
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public Personagem ParsePdf(Stream pdfStream)
        {
            var p = new Personagem
            {
                Atributo = new Atributo(),
                PersonagemCombate = new PersonagemCombate(),
                PersonagemDetalhes = new PersonagemDetalhes(),
                PersonagemDinheiro = new PersonagemDinheiro(),
                PersonagemConjuracao = new PersonagemConjuracao(),
                PersonagemPlayer = new PersonagemPlayer(),
                PersonagemItens = new List<PersonagemItem>(),
                CriadoEm = DateTime.UtcNow,
                Ativo = true,
                CampanhaId = 3, // Valid ID in the local DB
                CriadoPorId = 1,
                TipoId = 1
            };

            using var doc = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);
            if (doc.AcroForm == null) throw new Exception("O PDF não contém campos de formulário (AcroForm).");

            var fields = doc.AcroForm.Fields;
            bool isCustom55e = fields.Names.Contains("Esquerda Centro Single 12");

            string GetString(string name)
            {
                try { return fields[name]?.Value?.ToString() ?? ""; } 
                catch { return ""; }
            }
            int GetInt(string name) => int.TryParse(GetString(name), out int result) ? result : 0;

            if (isCustom55e)
            {
                p.Nome = GetString("Esquerda Centro Single 12");
                p.Descricao = "Importado do PDF 5.5e Personalizado";

                p.PersonagemPlayer.Classe = GetString("Esquerda Centro Single 12_3");
                p.PersonagemPlayer.Raca = GetString("Esquerda Centro Single 12_5");
                p.PersonagemPlayer.Antecedente = GetString("Esquerda Centro Single 12_2");
                p.PersonagemPlayer.Nivel = GetInt("N#C3#BAmeros Centralizados HV 8 Single_2");
                p.PersonagemPlayer.Alinhamento = GetString("Caixa de texto 8");
                p.PersonagemPlayer.Subclasse = GetString("Esquerda Centro Single 12_4");
                p.PersonagemPlayer.Tamanho = GetString("N#C3#BAmeros Centralizados HV 10 Single");
                p.PersonagemPlayer.Xp = GetInt("N#C3#BAmeros Centralizados HV 8 Single_3");

                if (p.PersonagemPlayer.Nivel == 0) p.PersonagemPlayer.Nivel = 1;
                if (string.IsNullOrEmpty(p.PersonagemPlayer.Alinhamento)) p.PersonagemPlayer.Alinhamento = "Desconhecido";
                if (string.IsNullOrEmpty(p.PersonagemPlayer.Subclasse)) p.PersonagemPlayer.Subclasse = "Desconhecido";
                if (string.IsNullOrEmpty(p.PersonagemPlayer.Tamanho)) p.PersonagemPlayer.Tamanho = "Médio";

                p.PersonagemDetalhes.CaracteristicasClasse = GetString("Multi esquerda 10");
                p.PersonagemDetalhes.HistoriaPersonalidade = $"Traços de Espécie: {GetString("Multi esquerda 10_3")}\nTalentos: {GetString("Multi esquerda 10_4")}\nHistória: {GetString("Caixa de texto 6_2")}";

                p.Atributo.Forca = GetInt("N#C3#BAmeros Centralizados HV 8 Single_15");
                p.Atributo.Destreza = GetInt("N#C3#BAmeros Centralizados HV 8 Single_13");
                p.Atributo.Constituicao = GetInt("N#C3#BAmeros Centralizados HV 8 Single_11");
                p.Atributo.Inteligencia = GetInt("N#C3#BAmeros Centralizados HV 8 Single_5");
                p.Atributo.Sabedoria = GetInt("N#C3#BAmeros Centralizados HV 8 Single_7");
                p.Atributo.Carisma = GetInt("N#C3#BAmeros Centralizados HV 8 Single_9");

                p.PersonagemCombate.ClasseArmadura = GetInt("N#C3#BAmeros Centralizados HV 8 Single_4");
                p.PersonagemCombate.Iniciativa = GetInt("N#C3#BAmeros Centralizados HV 12 Single_2");
                p.PersonagemCombate.Deslocamento = GetString("N#C3#BAmeros Centralizados HV 12 Single_3");
                p.PersonagemCombate.VidaMaxima = GetInt("Esquerda baixo 12_4");
                p.PersonagemCombate.VidaAtual = GetInt("Esquerda baixo 12");
                p.PersonagemCombate.VidaTemporaria = GetInt("Esquerda baixo 12_5");

                var equip = GetString("Caixa de texto 6_4");
                if (!string.IsNullOrEmpty(equip)) {
                    p.PersonagemItens.Add(new PersonagemItem { Nome = equip, TipoItem = "Equipamentos", Raridade = "Comum" });
                }
            }
            else
            {
                p.Nome = GetString("CharacterName");
                if (string.IsNullOrEmpty(p.Nome)) p.Nome = GetString("CharacterName 2"); // fallback
                if (string.IsNullOrEmpty(p.Nome)) p.Nome = "Personagem Desconhecido";
                p.Descricao = "Importado do PDF 5e Padrão";

                p.PersonagemPlayer.Classe = GetString("ClassLevel");
                p.PersonagemPlayer.Raca = GetString("Race ");
                p.PersonagemPlayer.Antecedente = GetString("Background");
                p.PersonagemPlayer.Alinhamento = GetString("Alignment");
                p.PersonagemPlayer.Nivel = 1;

                string pt = GetString("PersonalityTraits ");
                string idl = GetString("Ideals");
                string bnd = GetString("Bonds");
                string flw = GetString("Flaws");
                string bks = GetString("Backstory");
                p.PersonagemDetalhes.HistoriaPersonalidade = $"Traços: {pt}\nIdeais: {idl}\nVínculos: {bnd}\nDefeitos: {flw}\nHistória: {bks}";
                p.PersonagemDetalhes.CaracteristicasClasse = GetString("Features and Traits");

                p.Atributo.Forca = GetInt("STR");
                p.Atributo.Destreza = GetInt("DEX");
                p.Atributo.Constituicao = GetInt("CON");
                p.Atributo.Inteligencia = GetInt("INT");
                p.Atributo.Sabedoria = GetInt("WIS");
                p.Atributo.Carisma = GetInt("CHA");

                p.PersonagemCombate.ClasseArmadura = GetInt("AC");
                p.PersonagemCombate.Iniciativa = GetInt("Initiative");
                p.PersonagemCombate.Deslocamento = GetString("Speed");
                p.PersonagemCombate.VidaMaxima = GetInt("HPMax");
                p.PersonagemCombate.VidaAtual = GetInt("HPCurrent");
                p.PersonagemCombate.VidaTemporaria = GetInt("HPTemp");

                var equipText = GetString("Equipment");
                if (!string.IsNullOrEmpty(equipText)) {
                    p.PersonagemItens.Add(new PersonagemItem { Nome = equipText, TipoItem = "Equipamento Bruto", Raridade = "Comum" });
                }
            }

            return p;
        }
    }
}
