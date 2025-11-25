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

    // Chat Tables
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Prevent cascade delete cycles for Conversation participants.
        // If a User is deleted, conversations where they are UserA or UserB
        // will NOT be automatically deleted (Restrict behavior).

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

        base.OnModelCreating(modelBuilder);
    }
}
