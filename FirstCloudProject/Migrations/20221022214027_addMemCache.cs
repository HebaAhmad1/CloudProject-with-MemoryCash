using Microsoft.EntityFrameworkCore.Migrations;

namespace FirstCloudProject.Migrations
{
    public partial class addMemCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemoryCacheSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Hit = table.Column<int>(type: "int", nullable: false),
                    Miss = table.Column<int>(type: "int", nullable: false),
                    TotalSizeOfItems = table.Column<long>(type: "bigint", nullable: false),
                    TotalItemsNum = table.Column<int>(type: "int", nullable: false),
                    NumberOfRequests = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryCacheSettings", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemoryCacheSettings");
        }
    }
}
