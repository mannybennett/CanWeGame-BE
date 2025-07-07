using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; // Required for ValueConverter
using Microsoft.EntityFrameworkCore.ChangeTracking; // Required for ValueComparer
using System.Text.Json; // Required for JsonSerializer
using CanWeGame.API.Models;

namespace CanWeGame.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Schedule> Schedules { get; set; } = null!;
        public DbSet<Friendship> Friendships { get; set; } = null!; // I have a database table named Friendships that stores data according to the Friendship C# model.

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Model Configurations
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasMany(u => u.Schedules)
                .WithOne(gs => gs.User)
                .HasForeignKey(gs => gs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Friendship Model Configurations
            modelBuilder.Entity<Friendship>()
                .HasKey(f => new { f.UserId1, f.UserId2 });

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.User1)
                .WithMany(u => u.SentFriendships)
                .HasForeignKey(f => f.UserId1)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.User2)
                .WithMany(u => u.ReceivedFriendships)
                .HasForeignKey(f => f.UserId2)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Schedule Model Configurations ---
            // Configure ValueConverter for DaysOfWeek (List<string> to JSON string)
            modelBuilder.Entity<Schedule>()
                .Property(s => s.DaysOfWeek)
                .HasConversion(
                    new ValueConverter<List<string>, string>(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), // Convert List<string> to JSON string, "I know I'm passing null, & I confirm it to be treated as a null of this nullable type"
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>() // Convert JSON string to List<string>
                    )
                );

            // Configure ValueComparer for DaysOfWeek - separate method call
            modelBuilder.Entity<Schedule>()
                .Property(s => s.DaysOfWeek)
                .Metadata
                .SetValueComparer(
                    new ValueComparer<List<string>>(
                        // Comparison logic: Check if both lists are null, or if both are not null and their sorted elements are equal.
                        (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.OrderBy(s => s).SequenceEqual(c2.OrderBy(s => s))),
                        // Hashing logic: Combine hash codes of individual elements for efficient comparison in sets/dictionaries.
                        c => c.Aggregate(0, (hash, str) => HashCode.Combine(hash, str.GetHashCode())),
                        // Snapshotting logic: Create a new List<string> with the same elements (deep clone for value types like string).
                        c => c.ToList()
                    )
                );
        }
    }
}