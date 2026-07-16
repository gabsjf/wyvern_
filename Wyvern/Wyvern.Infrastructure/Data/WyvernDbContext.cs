using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
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
        public DbSet<Atributo> Atributos { get; set; }
        public DbSet<PersonagemDetalhes> PersonagemDetalhes { get; set; }
        public DbSet<PersonagemDinheiro> PersonagemDinheiros { get; set; }
        public DbSet<PersonagemAtaque> PersonagemAtaques { get; set; }

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
            // 4. Mapeamento Manual de Nomes de Tabela
            modelBuilder.Entity<Usuario>().ToTable("Usuario");

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