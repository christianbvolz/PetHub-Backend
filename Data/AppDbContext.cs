using Microsoft.EntityFrameworkCore;
using pethub.Models;

namespace pethub.Data;

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

    // Chat Tables
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}
