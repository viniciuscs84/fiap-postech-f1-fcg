using FCG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FCG.Migrations.Migrations;

/// <summary>Seeds the initial administrative account used to operate the Phase 1 API.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260901112000_SeedInitialData")]
public sealed class SeedInitialData : Migration
{
    private static readonly Guid AdministratorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime CreatedAtUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "Users",
            columns:
            [
                "Id",
                "Name",
                "Email",
                "NormalizedEmail",
                "PasswordHash",
                "Role",
                "CreatedAtUtc"
            ],
            values:
            [
                AdministratorId,
                "Administrator",
                "admin@example.com",
                "ADMIN@EXAMPLE.COM",
                "AQAAAAIAAYagAAAAEAARIjNEVWZ3iJmqu8zd7v+9B/m68fN7pVomQZIk2QhdaT58yv9z9zo2k0tHeC3mUA==",
                "Administrator",
                CreatedAtUtc
            ]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "Users",
            keyColumn: "Id",
            keyValue: AdministratorId);
    }
}
