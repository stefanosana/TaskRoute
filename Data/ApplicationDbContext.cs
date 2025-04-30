using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskRoute.Models;

namespace TaskRoute.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<Commission> Commissions { get; set; }
        public DbSet<Location> Locations { get; set; }

        public DbSet<UserTask> UserTasks { get; set; } 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configura la relazione tra Task e Location
            modelBuilder.Entity<Commission>()
                .HasOne(t => t.Location)
                .WithMany()
                .HasForeignKey(t => t.LocationId)
                .OnDelete(DeleteBehavior.SetNull); // Se la location viene eliminata, imposta LocationId a null

            // Configura la relazione tra Task e IdentityUser
            modelBuilder.Entity<Commission>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Impedisce l'eliminazione automatica dei task se l'utente viene eliminato
        }
    }
}