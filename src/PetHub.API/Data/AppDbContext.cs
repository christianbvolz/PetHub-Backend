using Microsoft.EntityFrameworkCore;
using PetHub.API.Models;

namespace PetHub.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Main Tables
    public DbSet<User> Users { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<PetImage> PetImages { get; set; }
    public DbSet<PetFavorite> PetFavorites { get; set; }
    public DbSet<Species> Species { get; set; }
    public DbSet<Breed> Breeds { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<PetTag> PetTags { get; set; }

    // Authentication
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuthToken> AuthTokens { get; set; }

    // Adoption Tables
    public DbSet<AdoptionRequest> AdoptionRequests { get; set; }

    // Chat Tables
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Configure Guid columns to use utf8mb4 collation (MySQL/MariaDB compatible) ---
        modelBuilder
            .Entity<User>()
            .Property(u => u.Id)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        modelBuilder.Entity<User>().Property(u => u.Cnpj).HasMaxLength(14);
        modelBuilder.Entity<User>().Property(u => u.Description).HasMaxLength(2000);

        modelBuilder
            .Entity<Pet>()
            .Property(p => p.UserId)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        modelBuilder
            .Entity<Conversation>()
            .Property(c => c.UserAId)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        modelBuilder
            .Entity<Conversation>()
            .Property(c => c.UserBId)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        modelBuilder
            .Entity<AdoptionRequest>()
            .Property(a => a.AdopterId)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        modelBuilder
            .Entity<ChatMessage>()
            .Property(cm => cm.SenderId)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        modelBuilder
            .Entity<PetFavorite>()
            .Property(pf => pf.UserId)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        // --- Pet Relationships ---
        modelBuilder
            .Entity<Pet>()
            .HasOne(p => p.User)
            .WithMany(u => u.Pets)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade); // If a user is deleted, their pets are also deleted.

        modelBuilder
            .Entity<Pet>()
            .HasOne(p => p.Species)
            .WithMany() // A species can have many pets, but we don't need a navigation property on Species for it.
            .HasForeignKey(p => p.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete a species if pets are associated with it.

        modelBuilder
            .Entity<Pet>()
            .HasOne(p => p.Breed)
            .WithMany() // A breed can have many pets, no navigation property needed on Breed.
            .HasForeignKey(p => p.BreedId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete a breed if pets are associated with it.

        // --- Breed-Species Relationship ---
        modelBuilder
            .Entity<Breed>()
            .HasOne(b => b.Species)
            .WithMany(s => s.Breeds) // A species has a list of breeds.
            .HasForeignKey(b => b.SpeciesId)
            .OnDelete(DeleteBehavior.Cascade); // If a species is deleted, its breeds are also deleted.

        // --- Chat Relationships ---
        modelBuilder
            .Entity<Conversation>()
            .HasOne(c => c.UserA)
            .WithMany()
            .HasForeignKey(c => c.UserAId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<Conversation>()
            .HasOne(c => c.UserB)
            .WithMany()
            .HasForeignKey(c => c.UserBId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<Conversation>()
            .HasOne(c => c.Pet)
            .WithMany()
            .HasForeignKey(c => c.PetId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder
            .Entity<Conversation>()
            .HasOne(c => c.AdoptionRequest)
            .WithMany()
            .HasForeignKey(c => c.AdoptionRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        // One inbox thread per pet between the same two users (UserAId < UserBId)
        modelBuilder
            .Entity<Conversation>()
            .HasIndex(c => new
            {
                c.PetId,
                c.UserAId,
                c.UserBId,
            })
            .IsUnique();

        modelBuilder
            .Entity<ChatMessage>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<ChatMessage>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessage>().HasIndex(m => new { m.ConversationId, m.SentAt });

        // --- Notification Relationships ---
        modelBuilder
            .Entity<Notification>()
            .Property(n => n.UserId)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        modelBuilder
            .Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.CreatedAt });
        modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });

        // --- PetTag Many-to-Many Relationship ---
        modelBuilder.Entity<PetTag>().HasKey(pt => new { pt.PetId, pt.TagId });

        modelBuilder
            .Entity<PetTag>()
            .HasOne(pt => pt.Pet)
            .WithMany(p => p.PetTags)
            .HasForeignKey(pt => pt.PetId);

        modelBuilder
            .Entity<PetTag>()
            .HasOne(pt => pt.Tag)
            .WithMany(t => t.PetTags)
            .HasForeignKey(pt => pt.TagId);

        // Configure Pet.CreatedAt to be set only on insert and never updated
        modelBuilder
            .Entity<Pet>()
            .Property(p => p.CreatedAt)
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(
                Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore
            );

        // Configure PetFavorite.FavoritedAt to be set only on insert and never updated
        modelBuilder
            .Entity<PetFavorite>()
            .Property(pf => pf.FavoritedAt)
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(
                Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore
            );

        // RefreshToken configuration
        modelBuilder
            .Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure RefreshToken.UserId to use the same char(36) + utf8mb4 collation as User.Id
        modelBuilder
            .Entity<RefreshToken>()
            .Property(rt => rt.UserId)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        modelBuilder
            .Entity<AuthToken>()
            .HasOne(t => t.User)
            .WithMany(u => u.AuthTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<AuthToken>()
            .Property(t => t.UserId)
            .HasColumnType("char(36)")
            .HasCharSet("utf8mb4")
            .UseCollation("utf8mb4_bin");

        modelBuilder.Entity<AuthToken>().HasIndex(t => t.TokenHash).IsUnique();
        modelBuilder.Entity<AuthToken>().HasIndex(t => new { t.UserId, t.Purpose });
    }
}
