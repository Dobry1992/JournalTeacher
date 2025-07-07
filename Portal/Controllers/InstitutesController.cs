using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Models.Model;
using Portal.Services;
using Portal.ViewModel;
using Portal.ViewModel.Raiting;
using Portal.ViewModel.Statement;

namespace Portal
{
    public class InstitutesController : Controller
    {
        private readonly AcademyContext _context;
        private readonly StudentAverageMarkService _studentAverageMarkService;
        private readonly InstituteAverageMarkService _instituteAverage;

        public InstitutesController(AcademyContext context, StudentAverageMarkService studentAverageMarkService, InstituteAverageMarkService instituteAverage)
        {
            _context = context;
            _studentAverageMarkService = studentAverageMarkService;
            _instituteAverage = instituteAverage;
        }

        public async Task<IActionResult> Statement(int id)
        {
            var institute = await _context.Institutes.FindAsync(id);
            if (institute == null)
                return NotFound("Институт не найден.");

            var groups = await _context.Groups
                .Where(g => g.InstituteID == id)
                .OrderBy(g => g.Name)
                .ToListAsync();

            if (!groups.Any())
                return View(new List<ViewModel.Statement.GroupRaiting>());

            var typeIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая оценка");
            if (typeIO == null)
                return NotFound("Тип 'Итоговая оценка' не найден.");

            var groupIds = groups.Select(g => g.GroupID).ToList();

            var students = await _context.Students
                .Where(s => groupIds.Contains(s.GroupID))
                .ToListAsync();

            var journals = await _context.Journals
                .Where(j => groupIds.Contains(j.GroupID))
                .ToListAsync();

            var subjectIds = journals.Select(j => j.SubjectID).Distinct().ToList();

            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.SubjectID))
                .ToDictionaryAsync(s => s.SubjectID);

            var studentIds = students.Select(s => s.StudentID).ToList();

            // Загрузка всех оценок как Mark
            var marks = await _context.Marks
                .Where(m =>
                    studentIds.Contains(m.StudentID) &&
                    subjectIds.Contains(m.SubjectID) &&
                    (m.FlagF == 0 || m.TypeOfExerciseID == typeIO.TypeOfExerciseID))
                .ToListAsync();

            // Группировки
            var studentsByGroup = students.GroupBy(s => s.GroupID).ToDictionary(g => g.Key, g => g.ToList());
            var journalsByGroup = journals.GroupBy(j => j.GroupID).ToDictionary(g => g.Key, g => g.ToList());
            var markLookup = marks
                .GroupBy(m => (m.StudentID, m.SubjectID))
                .ToDictionary(g => g.Key, g => g.ToList());

            List<ViewModel.Statement.GroupRaiting> groupRaitings = new();

            foreach (var group in groups)
            {
                var groupStudents = studentsByGroup.GetValueOrDefault(group.GroupID) ?? new List<Student>();
                var groupJournals = journalsByGroup.GetValueOrDefault(group.GroupID) ?? new List<Journal>();

                var groupSubjectIds = groupJournals
                    .Select(j => j.SubjectID)
                    .Distinct()
                    .Where(subjects.ContainsKey)
                    .ToList();

                var groupSubjects = groupSubjectIds
                    .Select(id => subjects[id])
                    .OrderBy(s => s.Name)
                    .ToList();

                List<StudRaiting> studRaitings = new();

                foreach (var student in groupStudents)
                {
                    List<SubRaiting> subRaitings = new();

                    foreach (var subject in groupSubjects)
                    {
                        var key = (student.StudentID, subject.SubjectID);
                        markLookup.TryGetValue(key, out var studentMarks);
                        studentMarks ??= new List<Mark>();

                        var finalMarks = studentMarks
                            .Where(m => m.TypeOfExerciseID == typeIO.TypeOfExerciseID)
                            .OrderByDescending(m => m.Date)
                            .ToList();

                        var regularMarks = studentMarks
                            .Where(m => m.FlagF == 0 && m.TypeOfExerciseID != typeIO.TypeOfExerciseID)
                            .ToList();

                        string rating;
                        string color;

                        if (finalMarks.Any() && !regularMarks.Any())
                        {
                            rating = finalMarks.First().Value;
                            color = "#ebc509";
                        }
                        else
                        {
                            var numericMarks = regularMarks
                                .Select(m => int.TryParse(m.Value, out int val) ? (int?)val : null)
                                .Where(v => v.HasValue)
                                .Select(v => v.Value)
                                .ToList();

                            rating = numericMarks.Any()
                                ? Math.Round(numericMarks.Average(), 3).ToString()
                                : "-";

                            color = "#FFFFFF";
                        }

                        subRaitings.Add(new SubRaiting
                        {
                            Subject = subject,
                            Raiting = rating,
                            Color = color,
                            FinalMarks = finalMarks
                        });
                    }

                    studRaitings.Add(new StudRaiting
                    {
                        Student = student,
                        SubRaitings = subRaitings
                    });
                }

                groupRaitings.Add(new ViewModel.Statement.GroupRaiting
                {
                    Group = group,
                    Subjects = groupSubjects,
                    Raitings = studRaitings
                });
            }

            ViewBag.Institute = institute;
            return View(groupRaitings);
        }


        public async Task<IActionResult> Start()
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

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Institutes
                .OrderBy(i => i.Arch)
                    .ThenBy(i => i.Name)
                .ToListAsync());
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH, ANB-HEAD, ANB-CI, ANB-ICDA, ANB-IST, User")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institute = await _context.Institutes.FindAsync(id);

            //Расчёт среднего бала за текущий семестр.
            string term;
            var date = DateTime.Now.AddYears(-1);
            var typeSZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Семинарское занятие");
            var typePZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Практическое занятие");
            var typeLZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лабораторное занятие");
            var typeL = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лекция");
            var typeKM = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Контрольное мероприятие");
            var typeGPZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Городское практическое занятие");
            List<double> marksAverage = new();
            List<Mark> marks = new();
            if (DateTime.Now.Month.ToString() == "9" || DateTime.Now.Month.ToString() == "10" || DateTime.Now.Month.ToString() == "11" || DateTime.Now.Month.ToString() == "12")
            {
                term = "первый семестр";
                marks = await _context.Marks
                    .Where(m =>
                        m.InstituteID == id &&
                        (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                        m.Date.Year == DateTime.Now.Year &&
                            (m.Date.Month.ToString() == "9" ||
                            m.Date.Month.ToString() == "10" ||
                            m.Date.Month.ToString() == "11" ||
                            m.Date.Month.ToString() == "12")
                    )
                    .ToListAsync();
                if (marks != null)
                {
                    foreach (var mark in marks)
                    {
                        if (double.TryParse(mark.Value, out var m))
                        {
                            marksAverage.Add(m);
                        }
                    }
                }
            }
            else if (DateTime.Now.Month.ToString() == "1")
            {
                term = "первый семестр";
                marks = await _context.Marks
                    .Where(m =>
                        m.InstituteID == id &&
                            (
                                m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typeL.TypeOfExerciseID
                            ) &&
                        ((m.Date.Year == DateTime.Now.Year &&
                        (m.Date.Month.ToString() == "1") ||
                            m.Date.Year == date.Year &&
                            (m.Date.Month.ToString() == "9" ||
                            m.Date.Month.ToString() == "10" ||
                            m.Date.Month.ToString() == "11" ||
                            m.Date.Month.ToString() == "12")))
                    )
                    .ToListAsync();
                if (marks != null)
                {
                    foreach (var mark in marks)
                    {
                        if (double.TryParse(mark.Value, out var m))
                        {
                            marksAverage.Add(m);
                        }
                    }
                }
            }
            else
            {
                term = "второй семестр";
                marks = await _context.Marks
                    .Where(m => m.InstituteID == id &&
                        (
                            m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                    ((m.Date.Year == DateTime.Now.Year &&
                        (m.Date.Month.ToString() == "2" ||
                        m.Date.Month.ToString() == "3" ||
                        m.Date.Month.ToString() == "4" ||
                        m.Date.Month.ToString() == "5" ||
                        m.Date.Month.ToString() == "6" ||
                        m.Date.Month.ToString() == "7" ||
                        m.Date.Month.ToString() == "8")))
                    )
                    .ToListAsync();
                if (marks != null)
                {
                    foreach (var mark in marks)
                    {
                        if (double.TryParse(mark.Value, out var m))
                        {
                            marksAverage.Add(m);
                        }
                    }
                }
            }

            //Количество обучающихся
            var students = _context.Students
                .Include(s => s.Group)
                .Where(s => s.InstituteID == id && s.Status == true)
                .AsNoTracking();
            int studentNumber = students.Count();

            //Рейтинг учащихся
            List<StudentRaiting> studentRaitings = new();
            foreach (var student in students)
            {
                double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, marks);

                if (studentRating == null)
                {
                    studentRating = 0;
                }

                StudentRaiting sr = new StudentRaiting()
                {
                    Group = student.Group,
                    Student = student,
                    Raiting = studentRating,
                };

                studentRaitings.Add(sr);
            }

            //Кол-во групп
            var groups = _context.Groups.Where(g => g.InstituteID == id && g.DateExit > DateTime.Now);

            //Кол-во специальностей
            var specialities = _context.Specialities.Where(s => s.InstituteID == id && s.Arch == false);

            //Рейтинг учебных групп
            List<InstGroupRaiting> groupsRating = new();
            foreach (var group in groups)
            {
                var groupStudents = students
                    .Where(s => s.GroupID == group.GroupID)
                    .ToList();

                var groupMarks = marks.
                    Where(m => m.GroupID == group.GroupID)
                    .ToList();

                List<double> groupStudentRatings = new();
                foreach (var student in groupStudents)
                {
                    double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, groupMarks);
                    if (studentRating == null)
                    {
                        studentRating = 0;
                    }
                    groupStudentRatings.Add((double)studentRating);
                }

                groupStudentRatings.RemoveAll(m => m == 0);
                double groupRating = 0;

                if (groupStudentRatings.Any())
                {
                    groupRating = Math.Round(groupStudentRatings.Average(), 3);
                }

                InstGroupRaiting instGroupRaiting = new()
                {
                    Group = group,
                    Raiting = groupRating
                };

                groupsRating.Add(instGroupRaiting);
            }

            //Средний балл института
            double instRating = 0;
            var filterGroupsRatig = groupsRating
                .Where(m => m.Raiting != 0)
                .ToList();
            if (filterGroupsRatig.Any())
            {
                instRating = Math.Round(filterGroupsRatig.Average(g => g.Raiting), 3);
            }

            //Оценочные показатели института
            Dictionary<int, int> marksNumber = new();
            Dictionary<int, decimal> marksPercent = new();
            for (int i = 1; i <= 10; i++)
            {
                if (marksAverage.Count != 0)
                {
                    decimal n1 = marksAverage.Where(x => x == i).Count();
                    decimal n2 = marksAverage.Count;
                    decimal mp = n1 / n2 * 100;
                    marksPercent.Add(i, Math.Round(mp, 3));
                }
                marksNumber.Add(i, marksAverage.Where(x => x == i).Count());
            }

            //Средняя месячная успеваемость
            Dictionary<string, string> raitingTime = new();
            var septemberMarks = marks.Where(m => m.Date.Month.ToString() == "9");
            List<Mark> sepMarks = new();
            foreach (var m in septemberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    sepMarks.Add(m);
                }
            }
            if (sepMarks.Count != 0)
            {
                raitingTime.Add("Сентябрь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(sepMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Сентябрь", "0");
            }

            var octoberMarks = marks.Where(m => m.Date.Month.ToString() == "10");
            List<Mark> octMarks = new();
            foreach (var m in octoberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    octMarks.Add(m);
                }
            }
            if (octMarks.Count != 0)
            {
                raitingTime.Add("Октябрь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(octMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Октябрь", "0");
            }

            var novemberMarks = marks.Where(m => m.Date.Month.ToString() == "11");
            List<Mark> novMarks = new();
            foreach (var m in novemberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    novMarks.Add(m);
                }
            }
            if (novMarks.Count != 0)
            {
                raitingTime.Add("Ноябрь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(novMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Ноябрь", "0");
            }


            var decemberMarks = marks.Where(m => m.Date.Month.ToString() == "12");
            List<Mark> decMarks = new();
            foreach (var m in decemberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    decMarks.Add(m);
                }
            }
            if (decMarks.Count != 0)
            {
                raitingTime.Add("Декабрь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(decMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Декабрь", "0");
            }

            var januaryMarks = marks.Where(m => m.Date.Month.ToString() == "1");
            List<Mark> janMarks = new();
            foreach (var m in januaryMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    janMarks.Add(m);
                }
            }
            if (janMarks.Count != 0)
            {
                raitingTime.Add("Январь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(janMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Январь", "0");
            }

            var februaryMarks = marks.Where(m => m.Date.Month.ToString() == "2");
            List<Mark> febMarks = new();
            foreach (var m in februaryMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    febMarks.Add(m);
                }
            }
            if (febMarks.Count != 0)
            {
                raitingTime.Add("Февраль", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(febMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Февраль", "0");
            }

            var marchMarks = marks.Where(m => m.Date.Month.ToString() == "3");
            List<Mark> marMarks = new();
            foreach (var m in marchMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    marMarks.Add(m);
                }
            }
            if (marMarks.Count != 0)
            {
                raitingTime.Add("Март", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(marMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Март", "0");
            }

            var aprilMarks = marks.Where(m => m.Date.Month.ToString() == "4");
            List<Mark> aprMarks = new();
            foreach (var m in aprilMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    aprMarks.Add(m);
                }
            }
            if (aprMarks.Count != 0)
            {
                raitingTime.Add("Апрель", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(aprMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Апрель", "0");
            }

            var mayMarks = marks.Where(m => m.Date.Month.ToString() == "5");
            List<Mark> mMarks = new();
            foreach (var m in mayMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    mMarks.Add(m);
                }
            }
            if (mMarks.Count != 0)
            {
                raitingTime.Add("Май", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(mMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Май", "0");
            }

            var juneMarks = marks.Where(m => m.Date.Month.ToString() == "6");
            List<Mark> junMarks = new();
            foreach (var m in juneMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    junMarks.Add(m);
                }
            }
            if (junMarks.Count != 0)
            {
                raitingTime.Add("Июнь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(junMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Июнь", "0");
            }

            var julyMarks = marks.Where(m => m.Date.Month.ToString() == "7");
            List<Mark> julMarks = new();
            foreach (var m in julyMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    julMarks.Add(m);
                }
            }
            if (julMarks.Count != 0)
            {
                raitingTime.Add("Июль", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(julMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Июль", "0");
            }

            var augustMarks = marks.Where(m => m.Date.Month.ToString() == "8");
            List<Mark> augMarks = new();
            foreach (var m in augustMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    augMarks.Add(m);
                }
            }
            if (augMarks.Count != 0)
            {
                raitingTime.Add("Август", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(augMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Август", "0");
            }

            //Учебный год
            string yearsStudy = "";
            if (DateTime.Now.Month.ToString() == "9" || DateTime.Now.Month.ToString() == "10" || DateTime.Now.Month.ToString() == "11" || DateTime.Now.Month.ToString() == "12")
            {
                DateTime dateTimePlus = DateTime.Now.AddYears(1);
                yearsStudy = DateTime.Now.Year + "/" + dateTimePlus.Year;
            }
            else
            {
                DateTime dateTimeMinus = DateTime.Now.AddYears(-1);
                yearsStudy = dateTimeMinus.Year + "/" + DateTime.Now.Year;
            }

            if (institute == null)
            {
                return NotFound();
            }

            var bestStudent = studentRaitings.OrderByDescending(s => s.Raiting).FirstOrDefault();
            if (bestStudent == null)
            {
                InstStudRaiting i = new();
                i.Raiting = 0;
                ViewBag.BestStudent = i;
            }
            else
            {
                if (bestStudent.Raiting == null)
                {
                    bestStudent.Raiting = 0;
                }
                ViewBag.BestStudent = bestStudent;
            }
            var worseStudent = studentRaitings.OrderByDescending(s => s.Raiting).LastOrDefault();
            if (worseStudent == null)
            {
                InstStudRaiting i = new();
                i.Raiting = 0;
                ViewBag.WorseStudent = i;
            }
            else
            {
                if (worseStudent.Raiting == null)
                {
                    worseStudent.Raiting = 0;
                }
                ViewBag.WorseStudent = worseStudent;
            }
            var bestGroup = groupsRating.OrderByDescending(g => g.Raiting).FirstOrDefault();
            if (bestGroup == null)
            {
                InstStudRaiting i = new();
                i.Raiting = 0;
                ViewBag.BestGroup = i;
            }
            else
            {
                ViewBag.BestGroup = bestGroup;
            }
            var worseGroup = groupsRating.OrderByDescending(g => g.Raiting).LastOrDefault();
            if (worseGroup == null)
            {
                InstStudRaiting i = new();
                i.Raiting = 0;
                ViewBag.WorseGroup = i;
            }
            else
            {
                ViewBag.WorseGroup = worseGroup;
            }

            ViewBag.Term = term;
            ViewBag.Raiting = Math.Round(instRating, 3);
            ViewBag.Students = studentNumber;
            ViewBag.Groups = groups.Count();
            ViewBag.Specialities = specialities.Count();
            ViewBag.StudentsRaiting = studentRaitings.OrderByDescending(s => s.Raiting);
            ViewBag.GroupsRaiting = groupsRating.OrderByDescending(g => g.Raiting);
            ViewBag.MarksNumber = marksNumber;
            ViewBag.MarksPercent = marksPercent;
            ViewBag.TimeRaiting = raitingTime;
            ViewBag.Year = yearsStudy;

            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InstituteID,Arch,Name,Role")] Institute institute)
        {
            if (ModelState.IsValid)
            {
                _context.Add(institute);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institute = await _context.Institutes.FindAsync(id);
            if (institute == null)
            {
                return NotFound();
            }
            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InstituteID,Arch,Name,Role")] Institute institute)
        {
            if (id != institute.InstituteID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(institute);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstituteExists(institute.InstituteID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institute = await _context.Institutes
                .FirstOrDefaultAsync(m => m.InstituteID == id);
            if (institute == null)
            {
                return NotFound();
            }

            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var institute = await _context.Institutes.FindAsync(id);
            _context.Institutes.Remove(institute);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institute = await _context.Institutes
                .FirstOrDefaultAsync(d => d.InstituteID == id);

            if (institute == null)
            {
                return NotFound();
            }

            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, [Bind("InstituteID,Name,Arch,Role")] Institute institute)
        {
            if (id != institute.InstituteID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var specialities = _context.Specialities
                        .Where(s => s.InstituteID == institute.InstituteID);

                    if (institute.Arch == true)
                    {
                        institute.Arch = false;
                        foreach (var speciality in specialities)
                        {
                            speciality.Arch = false;
                            _context.Specialities.Update(speciality);
                        }
                    }
                    else
                    {
                        institute.Arch = true;
                        foreach (var speciality in specialities)
                        {
                            speciality.Arch = true;
                            _context.Specialities.Update(speciality);
                        }
                    }
                    _context.Update(institute);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (!InstituteExists(institute.InstituteID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(institute);
        }

        private bool InstituteExists(int id)
        {
            return _context.Institutes.Any(e => e.InstituteID == id);
        }
    }
}
