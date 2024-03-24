using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Drawing;
using TaskScheduler.Models;

namespace TaskScheduler.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<MeetingTicket> MeetingTickets { get; set; }

        public DbSet <TaskTicket> TaskTickets { get; set; }
        
        public DbSet <DelegatedTaskTicket> DelegatedTaskTickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany(b => b.DelegatedTaskTickets)
                .WithMany(i => i.DelegatedTo)
                .UsingEntity(
                    "PostTag",
                    l => l.HasOne(typeof(DelegatedTaskTicket)).WithMany().HasForeignKey("TicketId").HasPrincipalKey(nameof(DelegatedTaskTicket.TicketId)),
                    r => r.HasOne(typeof(User)).WithMany().HasForeignKey("UserId").HasPrincipalKey(nameof(User.UserId)),
                    j => j.HasKey("UserId", "TicketId"));

            modelBuilder.Entity<MeetingTicket>();
            modelBuilder.Entity<TaskTicket>();
            modelBuilder.Entity<DelegatedTaskTicket>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
