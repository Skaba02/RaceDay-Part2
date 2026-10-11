using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.RoleName).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.Events)
                  .HasForeignKey(e => e.OrganiserId);

            entity.Property(e => e.DistanceKm)
                  .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId);

            entity.Property(c => c.DistanceKm)
                  .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            // Same rule as the Part 1 SQL script: a participant can only
            // enrol in the same category once.
            entity.HasIndex(en => new { en.ParticipantId, en.CategoryId }).IsUnique();

            entity.HasOne(en => en.Participant)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(en => en.ParticipantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(en => en.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(en => en.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasIndex(res => res.EnrolmentId).IsUnique();
            entity.HasOne(res => res.Enrolment)
                  .WithOne(en => en.Result)
                  .HasForeignKey<Result>(res => res.EnrolmentId);
        });

        // Seed the two roles so RoleId 1 = Organiser, 2 = Participant,
        // matching the Part 1 SQL script.
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" }
        );
    }
}