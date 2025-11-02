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
        public DbSet<Strand> Strands { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<TopicVersion> TopicVersions { get; set; }
        public DbSet<Content> Contents { get; set; }
        public DbSet<Formula> Formulas { get; set; }
        public DbSet<Example> Examples { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<Slide> Slides { get; set; }
        public DbSet<SlidePage> SlidePages { get; set; }
        public DbSet<SlideElement> SlideElements { get; set; }

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

            // Strands
            modelBuilder.Entity<Strand>(entity =>
            {
                entity.HasKey(e => e.StrandID);
                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.HasMany(s => s.Topics)
                    .WithOne(t => t.Strand)
                    .HasForeignKey(t => t.StrandID);
            });

            // Grades
            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasKey(e => e.GradeID);
                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.HasMany(g => g.Classes)
                    .WithOne(c => c.Grade)
                    .HasForeignKey(c => c.GradeID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Classes
            modelBuilder.Entity<Class>(entity =>
            {
                entity.HasKey(e => e.ClassID);
                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.HasMany(c => c.Topics)
                    .WithOne(t => t.Class)
                    .HasForeignKey(t => t.ClassID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Topics
            modelBuilder.Entity<Topic>(entity =>
            {
                entity.HasKey(e => e.TopicID);
                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.Objectives)
                    .HasColumnType("NVARCHAR(MAX)");
                entity.Property(e => e.Source)
                    .HasMaxLength(255);
                entity.HasMany(t => t.Contents)
                    .WithOne(c => c.Topic)
                    .HasForeignKey(c => c.TopicID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(t => t.TopicVersions)
                    .WithOne(tv => tv.Topic)
                    .HasForeignKey(tv => tv.TopicID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TopicVersions
            modelBuilder.Entity<TopicVersion>(entity =>
            {
                entity.HasKey(e => e.VersionID);
                entity.Property(e => e.Changes)
                    .HasColumnType("NVARCHAR(MAX)");
            });

            // Contents
            modelBuilder.Entity<Content>(entity =>
            {
                entity.HasKey(e => e.ContentID);
                entity.Property(e => e.Title)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.Summary)
                    .HasColumnType("NVARCHAR(MAX)");
                entity.HasMany(c => c.Formulas)
                    .WithOne(f => f.Content)
                    .HasForeignKey(f => f.ContentID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(c => c.Examples)
                    .WithOne(e => e.Content)
                    .HasForeignKey(e => e.ContentID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(c => c.Media)
                    .WithOne(m => m.Content)
                    .HasForeignKey(m => m.ContentID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Formulas
            modelBuilder.Entity<Formula>(entity =>
            {
                entity.HasKey(e => e.FormulaID);
                entity.Property(e => e.FormulaText)
                    .HasColumnType("NVARCHAR(MAX)")
                    .IsRequired();
                entity.Property(e => e.Explanation)
                    .HasColumnType("NVARCHAR(MAX)");
            });

            // Examples
            modelBuilder.Entity<Example>(entity =>
            {
                entity.HasKey(e => e.ExampleID);
                entity.Property(e => e.ExampleText)
                    .HasColumnType("NVARCHAR(MAX)")
                    .IsRequired();
            });

            // Media
            modelBuilder.Entity<Media>(entity =>
            {
                entity.HasKey(e => e.MediaID);
                entity.Property(e => e.Type)
                    .HasMaxLength(20)
                    .IsRequired();
                entity.Property(e => e.Url)
                    .HasMaxLength(2048)
                    .IsRequired();
                entity.Property(e => e.Description)
                    .HasMaxLength(255);
            });

            // Templates
            modelBuilder.Entity<Template>(entity =>
            {
                entity.HasKey(e => e.TemplateID);
                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.Description)
                    .HasMaxLength(500);
                entity.Property(e => e.ThumbnailUrl)
                    .HasMaxLength(2048)
                    .IsRequired();
                entity.Property(e => e.TemplatePath)
                    .HasMaxLength(2048)
                    .IsRequired();
                entity.Property(e => e.TemplateType)
                    .HasMaxLength(50);
                entity.Property(e => e.Tags)
                    .HasMaxLength(255);
            });

            // Slides
            modelBuilder.Entity<Slide>(entity =>
            {
                entity.HasKey(e => e.SlideID);
                entity.Property(e => e.Title)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("draft");
                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(s => s.SlidePages)
                    .WithOne(sp => sp.Slide)
                    .HasForeignKey(sp => sp.SlideID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SlidePages
            modelBuilder.Entity<SlidePage>(entity =>
            {
                entity.HasKey(e => e.PageID);
                entity.Property(e => e.Title)
                    .HasMaxLength(100);
                entity.HasMany(sp => sp.SlideElements)
                    .WithOne(se => se.SlidePage)
                    .HasForeignKey(se => se.PageID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SlideElements
            modelBuilder.Entity<SlideElement>(entity =>
            {
                entity.HasKey(e => e.ElementID);
                entity.Property(e => e.Type)
                    .HasMaxLength(20)
                    .IsRequired();
                entity.Property(e => e.Content)
                    .HasColumnType("NVARCHAR(MAX)");
                entity.HasOne(se => se.Formula)
                    .WithMany(f => f.SlideElements)
                    .HasForeignKey(se => se.FormulaID)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(se => se.Example)
                    .WithMany(e => e.SlideElements)
                    .HasForeignKey(se => se.ExampleID)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(se => se.Media)
                    .WithMany(m => m.SlideElements)
                    .HasForeignKey(se => se.MediaID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Indexes
            modelBuilder.Entity<User>().HasIndex(u => u.RoleID);
            modelBuilder.Entity<Topic>().HasIndex(t => t.ClassID);
            modelBuilder.Entity<Topic>().HasIndex(t => t.StrandID);
            modelBuilder.Entity<Content>().HasIndex(c => c.TopicID);
            modelBuilder.Entity<Formula>().HasIndex(f => f.ContentID);
            modelBuilder.Entity<Example>().HasIndex(e => e.ContentID);
            modelBuilder.Entity<Media>().HasIndex(m => m.ContentID);
            modelBuilder.Entity<Slide>().HasIndex(s => s.UserID);
            modelBuilder.Entity<Slide>().HasIndex(s => s.TopicID);
            modelBuilder.Entity<SlidePage>().HasIndex(sp => sp.SlideID);
            modelBuilder.Entity<SlideElement>().HasIndex(se => se.PageID);

            base.OnModelCreating(modelBuilder);
        }
    }
}
