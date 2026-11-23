using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Persistence;

public class LangCenterDbContext(DbContextOptions<LangCenterDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<TrainingClass> TrainingClasses => Set<TrainingClass>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AppUser>().HasIndex(user => user.NormalizedEmail).IsUnique();
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("students");
            entity.HasKey(student => student.Id);
            entity.Property(student => student.FullName).IsRequired().HasMaxLength(Student.MaxFullNameLength);
            entity.Property(student => student.Email).IsRequired().HasMaxLength(Student.MaxEmailLength);
        });
        modelBuilder.Entity<TrainingClass>(entity =>
        {
            entity.ToTable("training_classes", table =>
            {
                table.HasCheckConstraint("CK_training_classes_MinimumEnrollment", "\"MinimumEnrollment\" > 0");
                table.HasCheckConstraint("CK_training_classes_TuitionFee", "\"TuitionFee\" >= 0");
            });
            entity.HasKey(trainingClass => trainingClass.Id);
            entity.Property(trainingClass => trainingClass.Name).IsRequired().HasMaxLength(TrainingClass.MaxNameLength);
            entity.Property(trainingClass => trainingClass.TuitionFee).HasPrecision(14, 0);
            entity.Property(trainingClass => trainingClass.Status).HasConversion<string>().HasMaxLength(16);
        });
    }
}
