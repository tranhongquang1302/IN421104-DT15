using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Persistence;

public class LangCenterDbContext(DbContextOptions<LangCenterDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    internal const string StudentEmailIndexName = "IX_students_Email";
    internal const string RoomCodeIndexName = "IX_rooms_Code";
    internal const string InstructorCodeIndexName = "IX_instructors_Code";

    public DbSet<Student> Students => Set<Student>();
    public DbSet<TrainingClass> TrainingClasses => Set<TrainingClass>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ExamAdmission> ExamAdmissions => Set<ExamAdmission>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();

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
            entity.HasIndex(student => student.Email).IsUnique().HasDatabaseName(StudentEmailIndexName);
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
        modelBuilder.Entity<ExamAdmission>(entity =>
        {
            entity.ToTable("exam_admissions");
            entity.HasKey(admission => admission.Id);
            entity.HasOne<Enrollment>().WithMany()
                .HasForeignKey(admission => admission.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(admission => admission.EnrollmentId).IsUnique();
        });
        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.ToTable("certificates");
            entity.HasKey(certificate => certificate.Id);
            entity.Property(certificate => certificate.LookupCode).IsRequired().HasMaxLength(Certificate.LookupCodeLength);
            entity.HasOne<Enrollment>().WithMany()
                .HasForeignKey(certificate => certificate.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(certificate => certificate.EnrollmentId).IsUnique();
            entity.HasIndex(certificate => certificate.LookupCode).IsUnique();
        });
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");
            entity.HasKey(room => room.Id);
            entity.Property(room => room.Code).IsRequired().HasMaxLength(Room.MaxCodeLength);
            entity.Property(room => room.Name).IsRequired().HasMaxLength(Room.MaxNameLength);
            entity.HasIndex(room => room.Code).IsUnique().HasDatabaseName(RoomCodeIndexName);
        });
        modelBuilder.Entity<Instructor>(entity =>
        {
            entity.ToTable("instructors");
            entity.HasKey(instructor => instructor.Id);
            entity.Property(instructor => instructor.Code).IsRequired().HasMaxLength(Instructor.MaxCodeLength);
            entity.Property(instructor => instructor.FullName).IsRequired().HasMaxLength(Instructor.MaxFullNameLength);
            entity.HasIndex(instructor => instructor.Code).IsUnique().HasDatabaseName(InstructorCodeIndexName);
        });
        modelBuilder.Entity<ClassSession>(entity =>
        {
            entity.ToTable("class_sessions", table => table.HasCheckConstraint(
                "CK_class_sessions_TimeRange", "\"EndsAt\" > \"StartsAt\""));
            entity.HasKey(session => session.Id);
            entity.HasOne<TrainingClass>().WithMany()
                .HasForeignKey(session => session.TrainingClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Room>().WithMany()
                .HasForeignKey(session => session.RoomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Instructor>().WithMany()
                .HasForeignKey(session => session.InstructorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(session => session.StartsAt);
            entity.HasIndex(session => new { session.TrainingClassId, session.StartsAt });
            entity.HasIndex(session => new { session.RoomId, session.StartsAt });
            entity.HasIndex(session => new { session.InstructorId, session.StartsAt });
        });
    }
}
