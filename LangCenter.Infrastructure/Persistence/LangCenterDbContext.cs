using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Persistence;

public class LangCenterDbContext(DbContextOptions<LangCenterDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Student> Students => Set<Student>();

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
    }
}
