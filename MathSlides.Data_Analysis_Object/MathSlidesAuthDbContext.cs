using MathSlides.Business_Object.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Data_Analysis_Object
{
    public class MathSlidesAuthDbContext : DbContext
    {
        public MathSlidesAuthDbContext(DbContextOptions<MathSlidesAuthDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.HasIndex(e => e.Username).IsUnique();

                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(255)
                    .IsRequired();
                entity.Ignore(e => e.RoleName);
                entity.HasOne(u => u.Role)
          .WithMany()
          .HasForeignKey(u => u.RoleID);
            });

            // Roles
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleID);
                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = 1, Name = "Admin" },
                new Role { RoleID = 2, Name = "Teacher" },
                new Role { RoleID = 3, Name = "Student" }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}
