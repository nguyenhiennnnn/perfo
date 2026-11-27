using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema.Infrastructure.Migrations
{
    /// <summary>
    /// Áp trigger chặn UPDATE/DELETE/TRUNCATE trên "AuditLogs" ở mức DB (append-only),
    /// kể cả ExecuteUpdate/ExecuteDelete và SQL thô. Migration chỉ chạy SQL, không đổi model
    /// nên không có file Designer và không cần cập nhật AppDbContextModelSnapshot.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261127090000_AddAuditLogAppendOnlyTriggers")]
    public partial class AddAuditLogAppendOnlyTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AuditLogTriggers.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AuditLogTriggers.Down);
        }
    }
}
