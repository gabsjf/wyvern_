using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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

        public Personagem ParsePdf(Stream pdfStream, int campanhaId, int criadoPorId)
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
                CampanhaId = campanhaId,
                CriadoPorId = criadoPorId,
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
                return LerFichaPadrao(doc, campanhaId, criadoPorId);
            }
            return p;
        }

        // Ficha padrão de D&D 5e: lê os valores direto dos dicionários dos campos (mesma técnica da exportação)
        // e entrega ao FichaPdfLeitor, que faz o caminho inverso do FichaPdfMapper.
        private static Personagem LerFichaPadrao(PdfDocument doc, int campanhaId, int criadoPorId)
        {
            var acroForm = doc.Internals.Catalog.Elements.GetDictionary("/AcroForm")
                ?? throw new Exception("O PDF não contém campos de formulário (AcroForm).");

            var textos = new Dictionary<string, string>();
            var caixas = new Dictionary<string, bool>();
            var campos = PdfCampos.Coletar(acroForm);

            foreach (var nome in FichaPdfNomes())
            {
                if (!campos.TryGetValue(PdfCampos.NormalizarNome(nome), out var campo)) continue;
                if (nome.StartsWith("Check Box")) caixas[nome] = PdfCampos.Marcada(campo);
                else textos[nome] = PdfCampos.Texto(campo);
            }

            return FichaPdfLeitor.Ler(textos, caixas, campanhaId, criadoPorId);
        }

        // Todos os campos que o leitor conhece: o que o mapeador sabe escrever.
        private static IEnumerable<string> FichaPdfNomes()
        {
            var nomes = new List<string>
            {
                "CharacterName", "CharacterName 2", "ClassLevel", "Background", "Race", "Alignment", "XP",
                "ProfBonus", "AC", "Initiative", "Speed", "HPMax", "HPCurrent", "HPTemp", "HDTotal", "Inspiration",
                "CP", "SP", "EP", "GP", "PP", "Equipment", "Features and Traits", "ProficienciesLang", "Backstory", "Feat+Traits",
                "PersonalityTraits", "Ideals", "Bonds", "Flaws", "SpellcastingAbility 2",
                "Wpn Name", "Wpn Name 2", "Wpn Name 3", "Wpn1 AtkBonus", "Wpn2 AtkBonus", "Wpn3 AtkBonus",
                "Wpn1 Damage", "Wpn2 Damage", "Wpn3 Damage",
            };
            nomes.AddRange(FichaPdfMapper.CamposAtributos.Select(x => x.Pontuacao));
            nomes.AddRange(FichaPdfMapper.Pericias.Select(x => x.Campo));
            nomes.AddRange(FichaPdfMapper.Salvaguardas.Select(x => x.Campo));
            nomes.AddRange(FichaPdfMapper.Salvaguardas.Select(x => x.Caixa));
            nomes.AddRange(FichaPdfMapper.Pericias.Select(x => x.Caixa));
            nomes.AddRange(Enumerable.Range(12, 6).Select(i => $"Check Box {i}"));
            for (var n = 1; n <= 9; n++) { nomes.Add($"SlotsTotal {18 + n}"); nomes.Add($"SlotsRemaining {18 + n}"); }
            nomes.AddRange(FichaPdfMapper.LinhasDeMagia.Values.SelectMany(x => x));
            return nomes;
        }
    }
}
