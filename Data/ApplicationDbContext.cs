using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EduConnect.Models;

namespace EduConnect.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizResult> QuizResults { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<TopicProgress> TopicProgress { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<SemesterResult> SemesterResults { get; set; }
        public DbSet<StudentCourseProgress> StudentCourseProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Course>()
                .HasOne(c => c.Faculty)
                .WithMany(u => u.CreatedCourses)
                .HasForeignKey(c => c.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Topic relationship
            builder.Entity<Topic>()
                .HasOne(t => t.Course)
                .WithMany(c => c.Topics)
                .HasForeignKey(t => t.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Material>()
                .HasOne(m => m.Course)
                .WithMany(c => c.Materials)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Material>()
                .HasOne(m => m.Topic)
                .WithMany(t => t.Materials)
                .HasForeignKey(m => m.TopicId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<Quiz>()
                .HasOne(q => q.Course)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Quiz>()
                .HasOne(q => q.Topic)
                .WithMany(t => t.Quizzes)
                .HasForeignKey(q => q.TopicId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<QuizQuestion>()
                .HasOne(qq => qq.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(qq => qq.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuizResult>()
                .HasOne(qr => qr.Quiz)
                .WithMany(q => q.Results)
                .HasForeignKey(qr => qr.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuizResult>()
                .HasOne(qr => qr.Student)
                .WithMany(u => u.QuizResults)
                .HasForeignKey(qr => qr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Announcement>()
                .HasOne(a => a.Faculty)
                .WithMany()
                .HasForeignKey(a => a.FacultyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Announcement>()
                .HasOne(a => a.Course)
                .WithMany()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            // TopicProgress relationships
            builder.Entity<TopicProgress>()
                .HasOne(tp => tp.Student)
                .WithMany()
                .HasForeignKey(tp => tp.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TopicProgress>()
                .HasOne(tp => tp.Topic)
                .WithMany()
                .HasForeignKey(tp => tp.TopicId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.Entity<TopicProgress>()
                .HasOne(tp => tp.Material)
                .WithMany()
                .HasForeignKey(tp => tp.MaterialId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // Attendance relationships
            builder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Attendance>()
                .HasOne(a => a.Course)
                .WithMany()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // SemesterResult relationships
            builder.Entity<SemesterResult>()
                .HasOne(sr => sr.Student)
                .WithMany()
                .HasForeignKey(sr => sr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentCourseProgress relationships
            builder.Entity<StudentCourseProgress>()
                .HasOne(scp => scp.Student)
                .WithMany()
                .HasForeignKey(scp => scp.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentCourseProgress>()
                .HasOne(scp => scp.Course)
                .WithMany()
                .HasForeignKey(scp => scp.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
