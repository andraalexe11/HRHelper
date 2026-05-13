using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HRHelper.Models;

namespace HRHelper.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobPosition> JobPositions { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuizAttempt> QuizAttempts { get; set; }
    public DbSet<QuizAttemptQuestion> QuizAttemptQuestions { get; set; }
    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<QuizAttempt>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.RecruiterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAttempt>()
            .HasOne(a => a.JobPosition)
            .WithMany()
            .HasForeignKey(a => a.JobPositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAttemptQuestion>()
            .HasOne(s => s.QuizAttempt)
            .WithMany(a => a.SelectedQuestions)
            .HasForeignKey(s => s.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAttemptQuestion>()
            .HasOne(s => s.Question)
            .WithMany()
            .HasForeignKey(s => s.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuizAttemptAnswer>()
            .HasOne(s => s.QuizAttempt)
            .WithMany(a => a.Answers)
            .HasForeignKey(s => s.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAttemptAnswer>()
            .HasOne(s => s.Question)
            .WithMany()
            .HasForeignKey(s => s.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
