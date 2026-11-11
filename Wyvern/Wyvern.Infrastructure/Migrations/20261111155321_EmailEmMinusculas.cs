using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <summary>
    /// Os e-mails passam a ser guardados sem espaços e em minúsculas (EmailDaConta). Só muda dados, não o modelo.
    /// No Postgres, antes de mexer, confere se há contas que só diferem pela caixa do e-mail: se houver, para com
    /// um erro que lista os endereços, para decidir à mão qual conta fica (o índice único não deixaria as duas).
    /// </summary>
    public partial class EmailEmMinusculas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    DO $$
                    DECLARE repetidos text;
                    BEGIN
                        SELECT string_agg(e, ', ') INTO repetidos
                        FROM (SELECT lower(trim("Email")) AS e FROM "Usuario" GROUP BY lower(trim("Email")) HAVING count(*) > 1) d;
                        IF repetidos IS NOT NULL THEN
                            RAISE EXCEPTION 'Contas com o mesmo e-mail em caixas diferentes: %. Resolva antes de aplicar EmailEmMinusculas.', repetidos;
                        END IF;
                    END $$;
                    """);
            }

            migrationBuilder.Sql("""UPDATE "Usuario" SET "Email" = lower(trim("Email")) WHERE "Email" <> lower(trim("Email"));""");
            migrationBuilder.Sql("""UPDATE "Usuario" SET "EmailPendente" = lower(trim("EmailPendente")) WHERE "EmailPendente" IS NOT NULL AND "EmailPendente" <> lower(trim("EmailPendente"));""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Não dá para saber a caixa original; minúsculas continuam válidas com o código antigo.
        }
    }
}
