using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagementRewrite.Migrations
{
    /// <inheritdoc />
    public partial class CreateItemsFtsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE VIRTUAL TABLE ItemsFTS USING fts5(Name, Description, content='Items', content_rowid='Id');
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER AfterItemInsert AFTER INSERT ON Items BEGIN
                    INSERT INTO ItemsFTS(Rowid, Name, Description) VALUES (new.Id, new.Name, new.Description);
                END;
            ");
            
            migrationBuilder.Sql(@"
                CREATE TRIGGER AfterItemDelete AFTER DELETE ON Items BEGIN
                    INSERT INTO ItemsFTS(ItemsFTS, Rowid, Name, Description) VALUES ('delete', old.Id, old.Name, old.Description);
                END;
            ");
            
            migrationBuilder.Sql(@"
                CREATE TRIGGER AfterItemUpdate AFTER UPDATE ON Items BEGIN
                    INSERT INTO ItemsFTS(ItemsFTS, Rowid, Name, Description) VALUES ('delete', old.Id, old.Name, old.Description);
                    INSERT INTO ItemsFTS(Rowid, Name, Description) VALUES (new.Id, new.Name, new.Description);
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS AfterItemUpdate;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS AfterItemDelete;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS AfterItemInsert;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ItemsFTS;");
        }
    }
}
