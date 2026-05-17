using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsSummarizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "news_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    default_categories = table.Column<string>(type: "jsonb", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_fast_source = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    trust_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    last_fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    telegram_user_id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    first_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "news_articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    canonical_url = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    normalized_title = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    dedup_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    duplicate_of_article_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "New"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_news_articles_news_articles_duplicate_of_article_id",
                        column: x => x.duplicate_of_article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_news_articles_news_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "news_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "digests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    digest_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Created"),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digests", x => x.id);
                    table.ForeignKey(
                        name: "fk_digests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled_categories = table.Column<string>(type: "jsonb", nullable: false),
                    urgent_topics = table.Column<string>(type: "jsonb", nullable: false),
                    important_topics_text = table.Column<string>(type: "text", nullable: true),
                    excluded_topics_text = table.Column<string>(type: "text", nullable: true),
                    daily_digest_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    daily_digest_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    opportunity_digest_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    opportunity_digest_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    urgent_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    max_items_per_digest = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Europe/Moscow"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "article_ai_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    prompt_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    importance_score = table.Column<int>(type: "integer", nullable: false),
                    urgency_score = table.Column<int>(type: "integer", nullable: false),
                    opportunity_score = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    opportunity_reason = table.Column<string>(type: "text", nullable: true),
                    daily_digest_candidate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    opportunity_digest_candidate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    urgent_candidate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    raw_response = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_ai_results", x => x.id);
                    table.CheckConstraint("ck_article_ai_results_importance_score_range", "importance_score >= 0 AND importance_score <= 100");
                    table.CheckConstraint("ck_article_ai_results_opportunity_score_range", "opportunity_score >= 0 AND opportunity_score <= 100");
                    table.CheckConstraint("ck_article_ai_results_urgency_score_range", "urgency_score >= 0 AND urgency_score <= 100");
                    table.ForeignKey(
                        name: "fk_article_ai_results_news_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detailed_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    prompt_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    analysis_text = table.Column<string>(type: "text", nullable: true),
                    raw_response = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_detailed_analyses", x => x.id);
                    table.ForeignKey(
                        name: "fk_detailed_analyses_news_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_detailed_analyses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digest_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    digest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    title_snapshot = table.Column<string>(type: "text", nullable: false),
                    url_snapshot = table.Column<string>(type: "text", nullable: true),
                    source_name_snapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    summary_snapshot = table.Column<string>(type: "text", nullable: true),
                    reason_snapshot = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digest_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_digest_items_digests_digest_id",
                        column: x => x.digest_id,
                        principalTable: "digests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_digest_items_news_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: true),
                    digest_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dedup_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    title_snapshot = table.Column<string>(type: "text", nullable: true),
                    message_snapshot = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_digests_digest_id",
                        column: x => x.digest_id,
                        principalTable: "digests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notifications_news_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_ai_results_article_id",
                table: "article_ai_results",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_article_ai_results_article_id_provider_prompt_version",
                table: "article_ai_results",
                columns: new[] { "article_id", "provider", "prompt_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_article_ai_results_category",
                table: "article_ai_results",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_article_ai_results_daily_digest_candidate",
                table: "article_ai_results",
                column: "daily_digest_candidate");

            migrationBuilder.CreateIndex(
                name: "ix_article_ai_results_importance_score",
                table: "article_ai_results",
                column: "importance_score");

            migrationBuilder.CreateIndex(
                name: "ix_article_ai_results_opportunity_digest_candidate",
                table: "article_ai_results",
                column: "opportunity_digest_candidate");

            migrationBuilder.CreateIndex(
                name: "ix_article_ai_results_opportunity_score",
                table: "article_ai_results",
                column: "opportunity_score");

            migrationBuilder.CreateIndex(
                name: "ix_article_ai_results_urgency_score",
                table: "article_ai_results",
                column: "urgency_score");

            migrationBuilder.CreateIndex(
                name: "ix_article_ai_results_urgent_candidate",
                table: "article_ai_results",
                column: "urgent_candidate");

            migrationBuilder.CreateIndex(
                name: "ix_detailed_analyses_article_id",
                table: "detailed_analyses",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_detailed_analyses_expires_at",
                table: "detailed_analyses",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_detailed_analyses_status",
                table: "detailed_analyses",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_detailed_analyses_user_id",
                table: "detailed_analyses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_digest_items_article_id",
                table: "digest_items",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_digest_items_digest_id",
                table: "digest_items",
                column: "digest_id");

            migrationBuilder.CreateIndex(
                name: "ix_digest_items_digest_id_position",
                table: "digest_items",
                columns: new[] { "digest_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_digests_digest_type",
                table: "digests",
                column: "digest_type");

            migrationBuilder.CreateIndex(
                name: "ix_digests_sent_at",
                table: "digests",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "ix_digests_status",
                table: "digests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_digests_user_id",
                table: "digests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_digests_user_id_digest_type_period_start_period_end",
                table: "digests",
                columns: new[] { "user_id", "digest_type", "period_start", "period_end" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_canonical_url",
                table: "news_articles",
                column: "canonical_url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_content_hash",
                table: "news_articles",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_dedup_key",
                table: "news_articles",
                column: "dedup_key");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_duplicate_of_article_id",
                table: "news_articles",
                column: "duplicate_of_article_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_expires_at",
                table: "news_articles",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_fetched_at",
                table: "news_articles",
                column: "fetched_at");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_language",
                table: "news_articles",
                column: "language");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_normalized_title",
                table: "news_articles",
                column: "normalized_title");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_published_at",
                table: "news_articles",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_source_id",
                table: "news_articles",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_status",
                table: "news_articles",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_url",
                table: "news_articles",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_news_sources_is_enabled",
                table: "news_sources",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "ix_news_sources_is_fast_source",
                table: "news_sources",
                column: "is_fast_source");

            migrationBuilder.CreateIndex(
                name: "ix_news_sources_language",
                table: "news_sources",
                column: "language");

            migrationBuilder.CreateIndex(
                name: "ix_news_sources_url",
                table: "news_sources",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_article_id",
                table: "notifications",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_dedup_key",
                table: "notifications",
                column: "dedup_key");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_digest_id",
                table: "notifications",
                column: "digest_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_expires_at",
                table: "notifications",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_notification_type",
                table: "notifications",
                column: "notification_type");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_sent_at",
                table: "notifications",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_notification_type_dedup_key",
                table: "notifications",
                columns: new[] { "user_id", "notification_type", "dedup_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_preferences_user_id",
                table: "user_preferences",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_telegram_user_id",
                table: "users",
                column: "telegram_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_ai_results");

            migrationBuilder.DropTable(
                name: "detailed_analyses");

            migrationBuilder.DropTable(
                name: "digest_items");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "digests");

            migrationBuilder.DropTable(
                name: "news_articles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "news_sources");
        }
    }
}
