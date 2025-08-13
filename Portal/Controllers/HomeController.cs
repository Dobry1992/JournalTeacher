using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portal.Data;
using Portal.Models;
using Portal.Models.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AcademyContext _context;
        private readonly IWebHostEnvironment _appEnvironment;

        public HomeController(ILogger<HomeController> logger, AcademyContext academyContext, IWebHostEnvironment appEnvironment)
        {
            _logger = logger;
            _context = academyContext;
            _appEnvironment = appEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var institutes = _context.Institutes
                .Include(s => s.Specialities)
                    .ThenInclude(g => g.Groups)
                .AsNoTracking()
                .OrderBy(i => i.Name);

            var groupsArhive = _context.Groups
                .Include(s => s.Students)
                    .ThenInclude(s => s.Marks)
                .Include(s => s.Students)
                    .ThenInclude(s => s.StatementMarks)
                .Include(l => l.Lessons)
                .Include(l => l.StatementLessons)
                .Include(j => j.Journals)
                .Where(g => g.DateExit.AddMonths(1) <= DateTime.Now);

            List<ArhiveLesson> lessons = new();
            List<ArhiveStatementLesson> statementLessons = new();

            if (groupsArhive != null)
            {
                foreach (Group group in groupsArhive)
                {
                    GroupArhive ga = new();
                    ga.Name = group.Name;
                    ga.DateEnter = group.DateEnter;
                    ga.DateExit = group.DateExit;
                    ga.InstituteID = group.InstituteID;
                    ga.SpecialityID = group.SpecialityID;
                    _context.GroupArhives.Add(ga);
                    _context.SaveChanges();

                    foreach (Student student in group.Students)
                    {
                        StudentArhive sa = new();
                        sa.Name = student.Name;
                        sa.Surname = student.Surname;
                        sa.LastName = student.LastName;
                        sa.PlaceOfBirth = student.PlaceOfBirth;
                        sa.DateOfBirth = student.DateOfBirth;
                        sa.Status = false;
                        sa.InstituteID = student.InstituteID;
                        sa.GroupArhiveID = ga.GroupArhiveID;
                        _context.StudentArhives.Add(sa);
                        _context.SaveChanges();

                        foreach (Mark mark in student.Marks)
                        {
                            LessonArhive la = new();
                            Lesson lesson = _context.Lessons.Find(mark.LessonID);

                            if (lessons.FirstOrDefault(l => l.LessonID == lesson.LessonID) == null)
                            {
                                la.Date = lesson.Date;
                                la.Comment = lesson.Comment;
                                la.Signature = lesson.Signature;
                                la.FlagF = lesson.FlagF;
                                la.SubjectID = lesson.SubjectID;
                                la.ThemeID = lesson.ThemeID;
                                la.GroupArhiveID = ga.GroupArhiveID;
                                la.TypeOfExerciseID = lesson.TypeOfExerciseID;
                                _context.LessonArhives.Add(la);
                                _context.SaveChanges();

                                ArhiveLesson al = new();
                                al.LessonArhive = la;
                                al.LessonID = lesson.LessonID;
                                lessons.Add(al);
                            }
                            else
                            {
                                ArhiveLesson al = lessons.Find(l => l.LessonID == lesson.LessonID);
                                la = al.LessonArhive;
                            }

                            MarkArhive ma = new();
                            ma.Value = mark.Value;
                            ma.Date = mark.Date;
                            ma.Comment = mark.Comment;
                            ma.SignatureOfTeacher = mark.SignatureOfTeacher;
                            ma.HistoryOfMark = mark.HistoryOfMark;
                            ma.FlagF = mark.FlagF;
                            ma.InstituteID = mark.InstituteID;
                            ma.SubjectID = mark.SubjectID;
                            ma.GroupID = ga.GroupArhiveID;
                            ma.TypeOfExerciseID = mark.TypeOfExerciseID;
                            ma.DepartmentID = mark.DepartmentID;
                            ma.SpecialityID = mark.SpecialityID;
                            ma.ThemeID = mark.ThemeID;
                            ma.StudentArhiveID = sa.StudentArhiveID;
                            ma.LessonID = la.LessonArhiveID;
                            _context.MarkArhives.Add(ma);
                        }

                        foreach (StatementMark mark in student.StatementMarks)
                        {
                            StatementLessonArhive la = new();
                            StatementLesson lesson = _context.StatementLessons.Find(mark.StatementLessonID);

                            if (statementLessons.FirstOrDefault(l => l.StatementLessonID == lesson.StatementLessonID) == null)
                            {
                                la.Date = lesson.Date;
                                la.Comment = lesson.Comment;
                                la.Signature = lesson.Signature;
                                la.GroupArhiveID = ga.GroupArhiveID;
                                la.TypeOfExerciseID = lesson.TypeOfExerciseID;
                                _context.StatementLessonArhives.Add(la);
                                _context.SaveChanges();

                                ArhiveStatementLesson al = new();
                                al.StatementLesson = la;
                                al.StatementLessonID = lesson.StatementLessonID;
                                statementLessons.Add(al);
                            }
                            else
                            {
                                ArhiveStatementLesson al = statementLessons.Find(l => l.StatementLessonID == lesson.StatementLessonID);
                                la = al.StatementLesson;
                            }

                            StatementMarkArhive ma = new();
                            ma.Value = mark.Value;
                            ma.Date = mark.Date;
                            ma.Comment = mark.Comment;
                            ma.SignatureOfTeacher = mark.SignatureOfTeacher;
                            ma.HistoryOfMark = mark.HistoryOfMark;
                            ma.InstituteID = mark.InstituteID;
                            ma.GroupID = ga.GroupArhiveID;
                            ma.TypeOfExerciseID = mark.TypeOfExerciseID;
                            ma.SpecialityID = mark.SpecialityID;
                            ma.StudentArhiveID = sa.StudentArhiveID;
                            ma.StatementLessonID = la.StatementLessonArhiveID;
                            _context.StatementMarkArhives.Add(ma);
                        }
                    }

                    foreach (Journal journal in group.Journals)
                    {
                        JournalArhive ja = new();
                        ja.Comment = journal.Comment;
                        ja.GroupArhiveID = ga.GroupArhiveID;
                        ja.SubjectID = journal.SubjectID;
                        ja.Date = journal.Date;
                        _context.JournalArhives.Add(ja);
                        _context.SaveChanges();
                    }

                    _context.Groups.Remove(group);
                }
            }

            await _context.SaveChangesAsync();

            return View(await institutes.ToListAsync());
        }
    }
}
