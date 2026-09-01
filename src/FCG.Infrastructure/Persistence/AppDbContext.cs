using FCG.Domain.Users;
using FCG.Domain.Games;
using FCG.Domain.Library;
using FCG.Domain.Promotions;
using Microsoft.EntityFrameworkCore;

namespace FCG.Infrastructure.Persistence;

/// <summary>Central EF Core context for FCG persistence.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Gets the user accounts set.</summary>
    public DbSet<UserAccount> Users => Set<UserAccount>();

    /// <summary>Gets the catalog games set.</summary>
    public DbSet<Game> Games => Set<Game>();

    /// <summary>Gets the acquired games set.</summary>
    public DbSet<AcquiredGame> AcquiredGames => Set<AcquiredGame>();

    /// <summary>Gets the promotions set.</summary>
    public DbSet<Promotion> Promotions => Set<Promotion>();

    /// <summary>Configures the relational model and constraints.</summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(user => user.Id);

            builder.Property(user => user.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(user => user.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(320);

            builder.HasIndex(user => user.NormalizedEmail)
                .IsUnique();

            builder.Property(user => user.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(user => user.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(64);

            builder.Property(user => user.CreatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<Game>(builder =>
        {
            builder.ToTable("Games");
            builder.HasKey(game => game.Id);

            builder.Property(game => game.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(game => game.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(game => game.Genre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(game => game.CreatedByUserId)
                .IsRequired();

            builder.Property(game => game.CreatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<AcquiredGame>(builder =>
        {
            builder.ToTable("AcquiredGames");
            builder.HasKey(acquisition => acquisition.Id);

            builder.Property(acquisition => acquisition.UserId)
                .IsRequired();

            builder.Property(acquisition => acquisition.GameId)
                .IsRequired();

            builder.Property(acquisition => acquisition.AcquiredAtUtc)
                .IsRequired();

            builder.HasIndex(acquisition => new { acquisition.UserId, acquisition.GameId })
                .IsUnique();

            builder.HasOne(acquisition => acquisition.User)
                .WithMany()
                .HasForeignKey(acquisition => acquisition.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(acquisition => acquisition.Game)
                .WithMany()
                .HasForeignKey(acquisition => acquisition.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Promotion>(builder =>
        {
            builder.ToTable("Promotions");
            builder.HasKey(promotion => promotion.Id);

            builder.Property(promotion => promotion.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(promotion => promotion.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(promotion => promotion.NormalizedCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(promotion => promotion.NormalizedCode)
                .IsUnique();

            builder.Property(promotion => promotion.DiscountPercentage)
                .IsRequired()
                .HasPrecision(5, 2);

            builder.Property(promotion => promotion.StartsAtUtc)
                .IsRequired();

            builder.Property(promotion => promotion.EndsAtUtc)
                .IsRequired();

            builder.Property(promotion => promotion.CreatedByUserId)
                .IsRequired();

            builder.Property(promotion => promotion.CreatedAtUtc)
                .IsRequired();
        });
    }
}
