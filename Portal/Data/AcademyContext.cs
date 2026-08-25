using Microsoft.EntityFrameworkCore;
using Portal.Models;
using Portal.Models.Elective;

namespace Portal.Data
{
    public class AcademyContext: DbContext
    {
        public AcademyContext(DbContextOptions<AcademyContext> options) : base(options)
        {
        }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Theme> Themes { get; set; }
        public DbSet<TypeOfExercise> Types { get; set; }
        public DbSet<Mark> Marks { get; set; }
        public DbSet<Speciality> Specialities { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Journal> Journals { get; set; }
        public DbSet<Institute> Institutes { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<StatementLesson> StatementLessons { get; set; }
        public DbSet<StatementMark> StatementMarks { get; set; }
        public DbSet<Sub_SpecLink> Sub_SpecLinks { get; set; }
        public DbSet<GroupArhive> GroupArhives { get; set; }
        public DbSet<StudentArhive> StudentArhives { get; set; }
        public DbSet<MarkArhive> MarkArhives { get; set; }
        public DbSet<StatementMarkArhive> StatementMarkArhives { get; set; }
        public DbSet<LessonArhive> LessonArhives { get; set; }
        public DbSet<StatementLessonArhive> StatementLessonArhives { get; set; }
        public DbSet<JournalArhive> JournalArhives { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherNoPC> TeacherNoPCs { get; set; }
        public DbSet<Elective> Electives { get; set; }
        public DbSet<ElectiveTheme> ElectiveThemes { get; set; }
        public DbSet<ElectiveType> ElectiveTypes { get; set; }
        public DbSet<ElectiveLesson> ElectiveLessons { get; set; }
        public DbSet<ElectiveMark> ElectiveMarks { get; set; }
        public DbSet<El_Stud_Link> El_Stud_Links { get; set; }
        public DbSet<UnsatisfactoryMark> UnsatisfactoryMarks { get; set; }
        public DbSet<CompletedSubject>  CompletedSubjects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Group>().ToTable("Groups");
            modelBuilder.Entity<Student>().ToTable("Students");
            modelBuilder.Entity<Subject>().ToTable("Subjects");
            modelBuilder.Entity<Department>().ToTable("Departments");
            modelBuilder.Entity<Theme>().ToTable("Themes");
            modelBuilder.Entity<TypeOfExercise>().ToTable("TypeOfExercise");
            modelBuilder.Entity<Mark>().ToTable("Marks");
            modelBuilder.Entity<Speciality>().ToTable("Specialities");
            modelBuilder.Entity<Lesson>().ToTable("Lessons");
            modelBuilder.Entity<Journal>().ToTable("Journals");
            modelBuilder.Entity<Institute>().ToTable("Institutes");
            modelBuilder.Entity<Event>().ToTable("Events");
            modelBuilder.Entity<StatementLesson>().ToTable("StatementLessons");
            modelBuilder.Entity<StatementMark>().ToTable("StatementMarks");
            modelBuilder.Entity<Sub_SpecLink>().ToTable("Sub_SpecLinks");
            modelBuilder.Entity<GroupArhive>().ToTable("GroupArhives");
            modelBuilder.Entity<StudentArhive>().ToTable("StudentArhives");
            modelBuilder.Entity<MarkArhive>().ToTable("MarkArhives");
            modelBuilder.Entity<StatementMarkArhive>().ToTable("StatementMarkArhives");
            modelBuilder.Entity<LessonArhive>().ToTable("LessonArhives");
            modelBuilder.Entity<StatementLessonArhive>().ToTable("StatementLessonArhives");
            modelBuilder.Entity<JournalArhive>().ToTable("JournalArhives");
            modelBuilder.Entity<Teacher>().ToTable("Teachers");
            modelBuilder.Entity<TeacherNoPC>().ToTable("TeacherNoPCs");
            modelBuilder.Entity<Elective>().ToTable("Electives");
            modelBuilder.Entity<ElectiveTheme>().ToTable("ElectiveThemes");
            modelBuilder.Entity<ElectiveType>().ToTable("ElectiveTypes");
            modelBuilder.Entity<ElectiveLesson>().ToTable("ElectiveLessons");
            modelBuilder.Entity<ElectiveMark>().ToTable("ElectiveMarks");
            modelBuilder.Entity<El_Stud_Link>().ToTable("El_Stud_Links");
            modelBuilder.Entity<UnsatisfactoryMark>().ToTable("UnsatisfactoryMarks");
            modelBuilder.Entity<CompletedSubject>().ToTable("CompletedSubjects");
        }
    }
}
