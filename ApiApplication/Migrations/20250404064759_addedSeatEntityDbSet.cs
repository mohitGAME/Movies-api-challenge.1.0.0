using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiApplication.Migrations
{
    /// <inheritdoc />
    public partial class addedSeatEntityDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeatEntity_Auditoriums_AuditoriumId",
                table: "SeatEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_SeatEntity_Tickets_TicketEntityId",
                table: "SeatEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SeatEntity",
                table: "SeatEntity");

            migrationBuilder.RenameTable(
                name: "SeatEntity",
                newName: "Seats");

            migrationBuilder.RenameIndex(
                name: "IX_SeatEntity_TicketEntityId",
                table: "Seats",
                newName: "IX_Seats_TicketEntityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Seats",
                table: "Seats",
                columns: new[] { "AuditoriumId", "Row", "SeatNumber" });

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_Auditoriums_AuditoriumId",
                table: "Seats",
                column: "AuditoriumId",
                principalTable: "Auditoriums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_Tickets_TicketEntityId",
                table: "Seats",
                column: "TicketEntityId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seats_Auditoriums_AuditoriumId",
                table: "Seats");

            migrationBuilder.DropForeignKey(
                name: "FK_Seats_Tickets_TicketEntityId",
                table: "Seats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Seats",
                table: "Seats");

            migrationBuilder.RenameTable(
                name: "Seats",
                newName: "SeatEntity");

            migrationBuilder.RenameIndex(
                name: "IX_Seats_TicketEntityId",
                table: "SeatEntity",
                newName: "IX_SeatEntity_TicketEntityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SeatEntity",
                table: "SeatEntity",
                columns: new[] { "AuditoriumId", "Row", "SeatNumber" });

            migrationBuilder.AddForeignKey(
                name: "FK_SeatEntity_Auditoriums_AuditoriumId",
                table: "SeatEntity",
                column: "AuditoriumId",
                principalTable: "Auditoriums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SeatEntity_Tickets_TicketEntityId",
                table: "SeatEntity",
                column: "TicketEntityId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
