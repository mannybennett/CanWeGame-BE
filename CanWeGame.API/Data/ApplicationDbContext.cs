using Microsoft.EntityFrameworkCore;
using CanWeGame.API.Models; // Make sure to include your models namespace

namespace CanWeGame.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define your DbSets, which represent tables in your database
        public DbSet<User> Users { get; set; } = null!; // 'null!' tells the compiler it will be initialized
        public DbSet<Schedule> Schedules { get; set; } = null!;
        public DbSet<Friends> Friends { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Always call base method first

            // --- User Model Configurations ---
            // Ensure Username is unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Ensure Email is unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure the one-to-many relationship between User and GamingSchedule
            // A User has many GamingSchedules, and a GamingSchedule belongs to one User
            modelBuilder.Entity<User>()
                .HasMany(u => u.Schedules) // User has many GamingSchedules
                .WithOne(gs => gs.User)          // GamingSchedule has one User
                .HasForeignKey(gs => gs.UserId)  // The foreign key in GamingSchedule
                .OnDelete(DeleteBehavior.Cascade); // If a User is deleted, their schedules are also deleted

            // --- Friendship Model Configurations ---
            // Configure the composite primary key for Friendship
            modelBuilder.Entity<Friends>()
                .HasKey(f => new { f.UserId1, f.UserId2 }); // A friendship is uniquely identified by the pair of user IDs

            // Configure the relationship between User and Friendship (as UserId1)
            modelBuilder.Entity<Friends>()
                .HasOne(f => f.User1) // Friendship has one User1
                .WithMany(u => u.SentFriends) // User1 can initiate many friendships (calling it SentFriendships)
                .HasForeignKey(f => f.UserId1) // Foreign key to User.Id
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete if User1 is deleted (maintain integrity)

            // Configure the relationship between User and Friendship (as UserId2)
            modelBuilder.Entity<Friends>()
                .HasOne(f => f.User2) // Friendship has one User2
                .WithMany(u => u.ReceivedFriends) // User2 can be the recipient in many friendships (calling it ReceivedFriendships)
                .HasForeignKey(f => f.UserId2) // Foreign key to User.Id
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete if User2 is deleted

            // Add a unique constraint to ensure no duplicate friendship entries (e.g., (1,2) and (2,1) not both possible)
            // We'll enforce UserId1 < UserId2 in application logic to simplify.
            // EF Core will automatically make the composite key unique, but this extra constraint
            // ensures we don't accidentally create (1,2) and (2,1).
            // A more robust way to prevent (A,B) and (B,A) is to sort the IDs before storing.
            // For now, the composite key (UserId1, UserId2) ensures uniqueness for the pair in that order.
            // If you always store (smallerId, largerId), this handles duplicates.
        }
    }
}