
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsSummarizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "article_embeddings",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    dimensions = table.Column<int>(type: "integer", nullable: false),
                    text_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    embedding = table.Column<float[]>(type: "real[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_embeddings", x => x.article_id);
                    table.ForeignKey(
                        name: "fk_article_embeddings_news_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_embeddings_created_at",
                table: "article_embeddings",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_article_embeddings_model",
                table: "article_embeddings",
                column: "model");

            migrationBuilder.CreateIndex(
                name: "ix_article_embeddings_provider",
                table: "article_embeddings",
                column: "provider");

            migrationBuilder.CreateIndex(
                name: "ix_article_embeddings_text_hash",
                table: "article_embeddings",
                column: "text_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_embeddings");
        }
    }
}
