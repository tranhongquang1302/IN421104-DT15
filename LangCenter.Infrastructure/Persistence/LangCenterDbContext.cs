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
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Payment> Payments => Set<Payment>();

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
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("enrollments", table => table.HasCheckConstraint(
                "CK_enrollments_TuitionDue", "\"TuitionDue\" >= 0"));
            entity.HasKey(enrollment => enrollment.Id);
            entity.Property(enrollment => enrollment.TuitionDue).HasPrecision(14, 0);
            entity.HasOne<Student>().WithMany()
                .HasForeignKey(enrollment => enrollment.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TrainingClass>().WithMany()
                .HasForeignKey(enrollment => enrollment.TrainingClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(enrollment => new { enrollment.StudentId, enrollment.TrainingClassId }).IsUnique();
        });
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments", table => table.HasCheckConstraint("CK_payments_Amount", "\"Amount\" > 0"));
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Amount).HasPrecision(14, 0);
            entity.HasOne<Enrollment>().WithMany()
                .HasForeignKey(payment => payment.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(payment => new { payment.EnrollmentId, payment.RequestId }).IsUnique();
        });
    }
}
