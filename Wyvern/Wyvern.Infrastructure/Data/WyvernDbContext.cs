using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Wyvern.Domain.Entities;

namespace Wyvern.Infrastructure.Data
{
    public class WyvernDbContext : DbContext
    {
        public WyvernDbContext(DbContextOptions<WyvernDbContext> options) : base(options)
        {
        }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Sessao> Sessoes { get; set; }
        public DbSet<Anotacao> Anotacoes { get; set; }
        public DbSet<PastaAnotacao> PastasAnotacao { get; set; }

        public DbSet<PersonagemPlayer> PersonagemPlayers { get; set; }
        public DbSet<PersonagemPericia> PersonagemPericias { get; set; }
        public DbSet<PersonagemMagia> PersonagemMagias { get; set; }
        public DbSet<PersonagemItem> PersonagemItens { get; set; }
        public DbSet<PersonagemCombate> PersonagemCombates { get; set; }
        public DbSet<PersonagemConjuracao> PersonagemConjuracoes { get; set; }

        public DbSet<PersonagemNpc> PersonagensNpc { get; set; }
        public DbSet<Personagem> Personagens { get; set; }
        public DbSet<Pericia> Pericias { get; set; }
        public DbSet<Magia> Magias { get; set; }
        public DbSet<Item> Itens { get; set; }
        public DbSet<Campanha> Campanhas { get; set; }
        public DbSet<CampanhaMembro> CampanhasMembros { get; set; }
        public DbSet<Atributo> Atributos { get; set; }
        public DbSet<PersonagemDetalhes> PersonagemDetalhes { get; set; }
        public DbSet<PersonagemDinheiro> PersonagemDinheiros { get; set; }
        public DbSet<PersonagemAtaque> PersonagemAtaques { get; set; }

        public DbSet<EventoCampanha> EventosCampanha { get; set; }
        public DbSet<Cena> Cenas { get; set; }
        public DbSet<CenaImagem> CenaImagens { get; set; }
        public DbSet<TokenCena> TokensCena { get; set; }
        public DbSet<TokenImagem> TokenImagens { get; set; }
        public DbSet<ParedeCena> ParedesCena { get; set; }
        public DbSet<MarcadorCena> MarcadoresCena { get; set; }
        public DbSet<TracoCena> TracosCena { get; set; }
        public DbSet<Combate> Combates { get; set; }
        public DbSet<CombateParticipante> CombateParticipantes { get; set; }

        public DbSet<PersonagemAcaoPadrao> PersonagemAcoesPadrao { get; set; }
        public DbSet<PersonagemAcaoBonus> PersonagemAcoesBonus { get; set; }
        public DbSet<PersonagemReacao> PersonagemReacoes { get; set; }
        public DbSet<PersonagemAcaoLendaria> PersonagemAcoesLendarias { get; set; }
        public DbSet<PersonagemTracoEspecial> PersonagemTracosEspeciais { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<PersonagemPericia>()
                .HasKey(pp => new { pp.PersonagemId, pp.PericiaId });


            

            modelBuilder.Entity<Atributo>()
                .HasKey(a => a.PersonagemId);
            modelBuilder.Entity<Atributo>()
                .HasOne(a => a.Personagem)
                .WithOne(p => p.Atributo)
                .HasForeignKey<Atributo>(a => a.PersonagemId);

            modelBuilder.Entity<PersonagemCombate>()
                .HasKey(pc => pc.PersonagemId);
            modelBuilder.Entity<PersonagemCombate>()
                .HasOne(pc => pc.Personagem)
                .WithOne(p => p.PersonagemCombate)
                .HasForeignKey<PersonagemCombate>(pc => pc.PersonagemId);

            modelBuilder.Entity<PersonagemPlayer>()
                .HasKey(pp => pp.PersonagemId);
            modelBuilder.Entity<PersonagemPlayer>()
                .HasOne(pp => pp.personagem) 
                .WithOne(p => p.PersonagemPlayer)
                .HasForeignKey<PersonagemPlayer>(pp => pp.PersonagemId);

            modelBuilder.Entity<PersonagemDetalhes>()
                .HasKey(pd => pd.PersonagemId);
            modelBuilder.Entity<PersonagemDetalhes>()
                .HasOne(pd => pd.Personagem)
                .WithOne(p => p.PersonagemDetalhes)
                .HasForeignKey<PersonagemDetalhes>(pd => pd.PersonagemId);

            modelBuilder.Entity<PersonagemConjuracao>()
                .HasKey(pc => pc.PersonagemId);
            modelBuilder.Entity<PersonagemConjuracao>()
                .HasOne(pc => pc.Personagem)
                .WithOne(p => p.PersonagemConjuracao)
                .HasForeignKey<PersonagemConjuracao>(pc => pc.PersonagemId);

            modelBuilder.Entity<PersonagemDinheiro>()
                .HasKey(pd => pd.PersonagemId);
            modelBuilder.Entity<PersonagemDinheiro>()
                .HasOne(pd => pd.Personagem)
                .WithOne(p => p.PersonagemDinheiro)
                .HasForeignKey<PersonagemDinheiro>(pd => pd.PersonagemId);


          
            modelBuilder.Entity<Personagem>()
                .HasMany(p => p.PersonagemAtaques)
                .WithOne(pa => pa.Personagem)
                .HasForeignKey(pa => pa.PersonagemId);

            modelBuilder.Entity<PersonagemNpc>()
                .HasKey(pn => pn.PersonagemId);
            modelBuilder.Entity<PersonagemNpc>()
                .HasOne(pn => pn.Personagem)
                .WithOne(p => p.PersonagemNpc)
                .HasForeignKey<PersonagemNpc>(pn => pn.PersonagemId);

            modelBuilder.Entity<Personagem>()
                .HasMany(p => p.PersonagemAcoesPadrao)
                .WithOne(ap => ap.Personagem)
                .HasForeignKey(ap => ap.PersonagemId);

            modelBuilder.Entity<Personagem>()
                .HasMany(p => p.PersonagemAcoesBonus)
                .WithOne(ab => ab.Personagem)
                .HasForeignKey(ab => ab.PersonagemId);

            modelBuilder.Entity<Personagem>()
                .HasMany(p => p.PersonagemReacoes)
                .WithOne(pr => pr.Personagem)
                .HasForeignKey(pr => pr.PersonagemId);

            modelBuilder.Entity<Personagem>()
                .HasMany(p => p.PersonagemAcoesLendarias)
                .WithOne(al => al.Personagem)
                .HasForeignKey(al => al.PersonagemId);

            modelBuilder.Entity<Personagem>()
                .HasMany(p => p.PersonagemTracosEspeciais)
                .WithOne(te => te.Personagem)
                .HasForeignKey(te => te.PersonagemId);

            modelBuilder.Entity<Personagem>()
                .HasOne(p => p.CriadoPor)
                .WithMany()
                .HasForeignKey(p => p.CriadoPorId)
                .OnDelete(DeleteBehavior.Restrict); // O usuário é deletado, o personagem fica (ou você apaga na mão)

            modelBuilder.Entity<Campanha>()
                .HasOne(c => c.Mestre)
                .WithMany(u => u.Campanhas)
                .HasForeignKey(c => c.MestreId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CampanhaMembro>()
                .HasOne(cj => cj.Campanha)
                .WithMany(c => c.Membros)
                .HasForeignKey(cj => cj.CampanhaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampanhaMembro>()
                .HasOne(cj => cj.Usuario)
                .WithMany()
                .HasForeignKey(cj => cj.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Papel guardado como inteiro (1 = Mestre, 2 = Jogador)
            modelBuilder.Entity<CampanhaMembro>()
                .Property(m => m.Papel)
                .HasConversion<int>();

            // Autor da pasta; se o usuário sumir, a pasta fica sem autor (só o Mestre a altera)
            modelBuilder.Entity<PastaAnotacao>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(p => p.CriadoPorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PastaAnotacao>()
                .HasMany(p => p.Anotacoes)
                .WithOne(a => a.Pasta)
                .HasForeignKey(a => a.PastaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Anotacao>()
                .HasOne(a => a.Campanha)
                .WithMany()
                .HasForeignKey(a => a.CampanhaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Anotacao>()
                .HasOne(a => a.CriadoPor)
                .WithMany()
                .HasForeignKey(a => a.CriadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Crônica da campanha: some com a campanha; apagar uma sessão ou um usuário não apaga os eventos
            modelBuilder.Entity<EventoCampanha>()
                .HasOne(e => e.Campanha).WithMany().HasForeignKey(e => e.CampanhaId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EventoCampanha>()
                .HasOne(e => e.Sessao).WithMany().HasForeignKey(e => e.SessaoId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<EventoCampanha>()
                .HasOne(e => e.CriadoPor).WithMany().HasForeignKey(e => e.CriadoPorId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<EventoCampanha>()
                .HasIndex(e => new { e.CampanhaId, e.Ordem });

            // VTT: as cenas somem com a campanha; a imagem e os tokens somem com a cena. Apagar um personagem, um
            // combatente ou um usuário só solta o token (ele continua no mapa, sem o vínculo).
            modelBuilder.Entity<Cena>()
                .HasOne(c => c.Campanha).WithMany().HasForeignKey(c => c.CampanhaId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Cena>()
                .HasIndex(c => new { c.CampanhaId, c.Ordem });
            modelBuilder.Entity<CenaImagem>()
                .HasOne(i => i.Cena).WithOne().HasForeignKey<CenaImagem>(i => i.CenaId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Cena>().Property(c => c.MetrosPorQuadrado).HasDefaultValue(1.5);
            modelBuilder.Entity<Cena>().Property(c => c.LimitarDeslocamento).HasDefaultValue(true);
            modelBuilder.Entity<Cena>()
                .HasOne(c => c.Sessao).WithMany().HasForeignKey(c => c.SessaoId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<TokenImagem>()
                .HasOne(i => i.Token).WithOne().HasForeignKey<TokenImagem>(i => i.TokenId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ParedeCena>()
                .HasOne(p => p.Cena).WithMany().HasForeignKey(p => p.CenaId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MarcadorCena>()
                .HasOne(m => m.Cena).WithMany().HasForeignKey(m => m.CenaId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TracoCena>()
                .HasOne(t => t.Cena).WithMany().HasForeignKey(t => t.CenaId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TracoCena>()
                .HasOne(t => t.Usuario).WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<TokenCena>()
                .HasOne(t => t.Cena).WithMany(c => c.Tokens).HasForeignKey(t => t.CenaId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TokenCena>()
                .HasOne(t => t.Personagem).WithMany().HasForeignKey(t => t.PersonagemId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<TokenCena>()
                .HasOne(t => t.Participante).WithMany().HasForeignKey(t => t.ParticipanteId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<TokenCena>()
                .HasOne(t => t.ControladoPor).WithMany().HasForeignKey(t => t.ControladoPorId).OnDelete(DeleteBehavior.SetNull);

            // 4. Mapeamento Manual de Nomes de Tabela
            modelBuilder.Entity<Usuario>().ToTable("Usuario");

            // Integridade que antes só existia na checagem da aplicação (sujeita a corrida entre requisições)
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Campanha>()
                .HasIndex(c => c.TokenConvite)
                .IsUnique();

            modelBuilder.Entity<CampanhaMembro>()
                .HasIndex(cj => new { cj.CampanhaId, cj.UsuarioId })
                .IsUnique();

            // No máximo um combate ativo por sessão
            var filtroAtivo = Database.IsNpgsql() ? "\"Ativo\" = true" : "\"Ativo\" = 1";
            modelBuilder.Entity<Combate>()
                .HasIndex(c => c.SessaoId)
                .IsUnique()
                .HasFilter(filtroAtivo);

            // Concorrência otimista no combate (dois "próximo turno" simultâneos não se atropelam).
            // xmin é uma coluna de sistema do PostgreSQL: não gera coluna nova no banco.
            if (Database.IsNpgsql())
            {
                modelBuilder.Entity<Combate>()
                    .Property<uint>("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            }

            // 5. Seed Data
            modelBuilder.Entity<TipoPersonagem>().HasData(
                new TipoPersonagem { Id = 1, Nome = "Jogador" },
                new TipoPersonagem { Id = 2, Nome = "NPC" },
                new TipoPersonagem { Id = 3, Nome = "Monstro" }
            );

            modelBuilder.Entity<Pericia>().HasData(
                new Pericia { PericiaId = 1, Nome = "Acrobacia", Ativo = true },
                new Pericia { PericiaId = 2, Nome = "Arcanismo", Ativo = true },
                new Pericia { PericiaId = 3, Nome = "Atletismo", Ativo = true },
                new Pericia { PericiaId = 4, Nome = "Atuação", Ativo = true },
                new Pericia { PericiaId = 5, Nome = "Enganação", Ativo = true },
                new Pericia { PericiaId = 6, Nome = "Furtividade", Ativo = true },
                new Pericia { PericiaId = 7, Nome = "História", Ativo = true },
                new Pericia { PericiaId = 8, Nome = "Intimidação", Ativo = true },
                new Pericia { PericiaId = 9, Nome = "Intuição", Ativo = true },
                new Pericia { PericiaId = 10, Nome = "Investigação", Ativo = true },
                new Pericia { PericiaId = 11, Nome = "Lidar com Animais", Ativo = true },
                new Pericia { PericiaId = 12, Nome = "Medicina", Ativo = true },
                new Pericia { PericiaId = 13, Nome = "Natureza", Ativo = true },
                new Pericia { PericiaId = 14, Nome = "Percepção", Ativo = true },
                new Pericia { PericiaId = 15, Nome = "Persuasão", Ativo = true },
                new Pericia { PericiaId = 16, Nome = "Prestidigitação", Ativo = true },
                new Pericia { PericiaId = 17, Nome = "Religião", Ativo = true },
                new Pericia { PericiaId = 18, Nome = "Sobrevivência", Ativo = true }
            );

            base.OnModelCreating(modelBuilder);
        }


    }
}