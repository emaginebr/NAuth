using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NAuth.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "role_id_seq");

            migrationBuilder.CreateSequence(
                name: "user_addresses_id_seq");

            migrationBuilder.CreateSequence(
                name: "user_documents_id_seq");

            migrationBuilder.CreateSequence(
                name: "user_id_seq");

            migrationBuilder.CreateSequence(
                name: "user_phones_id_seq");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('role_id_seq'::regclass)"),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("roles_pkey", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('user_id_seq'::regclass)"),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    email = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    recovery_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    id_document = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    birth_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    pix_key = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    stripe_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    image = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "user_addresses",
                columns: table => new
                {
                    address_id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('user_addresses_id_seq'::regclass)"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    zip_code = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    address = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    complement = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    neighborhood = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    state = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_addresses", x => x.address_id);
                    table.ForeignKey(
                        name: "fk_user_address",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_documents",
                columns: table => new
                {
                    document_id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('user_documents_id_seq'::regclass)"),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    document_type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    base64 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_documents_pkey", x => x.document_id);
                    table.ForeignKey(
                        name: "fk_user_document",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_phones",
                columns: table => new
                {
                    phone_id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('user_phones_id_seq'::regclass)"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_phones_pkey", x => x.phone_id);
                    table.ForeignKey(
                        name: "fk_user_phone",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_roles_pkey", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_role_role",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id");
                    table.ForeignKey(
                        name: "fk_user_role_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_addresses_user_id",
                table: "user_addresses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_documents_user_id",
                table: "user_documents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_phones_user_id",
                table: "user_phones",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_addresses");

            migrationBuilder.DropTable(
                name: "user_documents");

            migrationBuilder.DropTable(
                name: "user_phones");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropSequence(
                name: "role_id_seq");

            migrationBuilder.DropSequence(
                name: "user_addresses_id_seq");

            migrationBuilder.DropSequence(
                name: "user_documents_id_seq");

            migrationBuilder.DropSequence(
                name: "user_id_seq");

            migrationBuilder.DropSequence(
                name: "user_phones_id_seq");
        }
    }
}
