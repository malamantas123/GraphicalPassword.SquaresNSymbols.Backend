using Microsoft.EntityFrameworkCore;

namespace Server;

public class ServerDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public ServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(x => x.UserId);
        modelBuilder.Entity<User>().Property(x => x.UserId).ValueGeneratedOnAdd();
        //modelBuilder.Entity<User>().Property(x => x.IncorrectLoginAttempts).HasDefaultValue(0);
    }
}