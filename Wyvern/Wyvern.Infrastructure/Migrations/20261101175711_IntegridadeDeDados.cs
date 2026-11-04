using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntegridadeDeDados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Limpeza antes de criar as restrições únicas, para a migration não falhar por dados antigos.
            // (Só PostgreSQL usa migrations; SQLite de desenvolvimento/testes é criado com EnsureCreated.)

            // Jogador que entrou duas vezes na mesma campanha: mantém só o registro mais antigo.
            migrationBuilder.Sql(@"
                DELETE FROM ""Campanhas_Jogadores""
                WHERE ""Id"" NOT IN (
                    SELECT MIN(""Id"") FROM ""Campanhas_Jogadores"" GROUP BY ""CampanhaId"", ""UsuarioId"");");

            // Mais de um combate ativo na mesma sessão: mantém ativo apenas o mais recente.
            migrationBuilder.Sql(@"
                UPDATE ""Combates"" SET ""Ativo"" = false
                WHERE ""Ativo"" = true
                  AND ""CombateId"" NOT IN (
                    SELECT MAX(""CombateId"") FROM ""Combates"" WHERE ""Ativo"" = true GROUP BY ""SessaoId"");");

            // E-mails repetidos não podem ser resolvidos automaticamente (não dá para escolher qual conta vale).
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM ""Usuario"" GROUP BY ""Email"" HAVING COUNT(*) > 1) THEN
                        RAISE EXCEPTION 'Existem usuarios com o mesmo e-mail. Resolva as duplicatas na tabela Usuario antes de aplicar esta migration.';
                    END IF;
                END $$;");
            migrationBuilder.DropIndex(
                name: "IX_Combates_SessaoId",
                table: "Combates");

            migrationBuilder.DropIndex(
                name: "IX_Campanhas_Jogadores_CampanhaId",
                table: "Campanhas_Jogadores");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Combates",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Email",
                table: "Usuario",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Combates_SessaoId",
                table: "Combates",
                column: "SessaoId",
                unique: true,
                filter: "\"Ativo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_Jogadores_CampanhaId_UsuarioId",
                table: "Campanhas_Jogadores",
                columns: new[] { "CampanhaId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_TokenConvite",
                table: "Campanhas",
                column: "TokenConvite",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuario_Email",
                table: "Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Combates_SessaoId",
                table: "Combates");

            migrationBuilder.DropIndex(
                name: "IX_Campanhas_Jogadores_CampanhaId_UsuarioId",
                table: "Campanhas_Jogadores");

            migrationBuilder.DropIndex(
                name: "IX_Campanhas_TokenConvite",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Combates");

            migrationBuilder.CreateIndex(
                name: "IX_Combates_SessaoId",
                table: "Combates",
                column: "SessaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_Jogadores_CampanhaId",
                table: "Campanhas_Jogadores",
                column: "CampanhaId");
        }
    }
}
