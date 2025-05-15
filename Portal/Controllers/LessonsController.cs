using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Repository;
using Portal.Services;

namespace Portal
{
    public class LessonsController : Controller
    {
        private readonly AcademyContext _context;
        private readonly UserNameService _userNameService;


        public LessonsController(AcademyContext context, UserNameService userNameService)
        {
            _context = context;
            _userNameService = userNameService;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index(DateTime date_1, DateTime date_2, int groupId, int subjectId, string teacher)
        {
            var lessons = await _context.Lessons
               .OrderBy(l => l.Date)
               .Include(l => l.Group)
               .Include(l => l.Theme)
                   .ThenInclude(t => t.Subject)
               .Include(l => l.TypeOfExercise)
               .ToListAsync();

            if (date_1.ToShortDateString() == "01.01.0001" && date_2.ToShortDateString() == "01.01.0001" && groupId == 0 && subjectId == 0 && teacher == null)
            {
                lessons = lessons
                    .Where(l => l.Date.Year == DateTime.Now.Year && l.Date.Month == DateTime.Now.Month && l.Date.Day == DateTime.Now.Day)
                    .ToList();
            }

            if (date_1.ToShortDateString() != "01.01.0001")
            {
                lessons = lessons
                    .Where(l => l.Date >= date_1)
                    .ToList();
            }

            if (date_2.ToShortDateString() != "01.01.0001")
            {
                lessons = lessons
                    .Where(l => l.Date <= date_2)
                    .ToList();
            }

            if (groupId != 0)
            {
                lessons = lessons
                    .Where(l => l.GroupID == groupId)
                    .ToList();
            }

            if (subjectId != 0)
            {
                lessons = lessons
                   .Where(l => l.SubjectID == subjectId)
                   .ToList();
            }

            if (teacher != null && teacher != "")
            {
                lessons = lessons
                  .Where(l => l.Signature == teacher)
                  .ToList();
            }

            var teachersMarks = _context.Marks.Where(m => m.SignatureOfTeacher != null);
            var teachers = from mark in teachersMarks
                           group mark by mark.SignatureOfTeacher into t
                           select new { NickName = t.Key };

            List<string> tchrs = new();
            foreach (var t in teachers)
            {
                tchrs.Add(t.NickName);
            }

            ViewBag.Groups = _context.Groups;
            ViewBag.Subjects = _context.Subjects;
            ViewBag.Teachers = tchrs;

            return View(lessons);
        }

        public IActionResult CreateF(int? GroupID, int? SubjectID)
        {
            string teacher = _userNameService.GetDisplayName();

            ViewData["ThemeID"] = new SelectList(
                _context.Themes.Where(t => t.SubjectID == SubjectID && t.Name == "Контрольное занятие"),
                "ThemeID", "Name");

            string[] allowedTypes =
                {
                    "Экзамен", "Дифференцированный зачёт", "Зачёт"
                };

            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => allowedTypes.Contains(t.Name)), "TypeOfExerciseID", "Name");

            ViewBag.UserName = teacher;
            ViewBag.Teachers = _context.Teachers.OrderBy(t => t.FamilyName);
            ViewBag.TeachersNoPC = _context.TeacherNoPCs.OrderBy(t => t.LastName).AsNoTracking();
            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateF(int GroupID, int SubjectID, [Bind("LessonID,Date,Comment,FlagF,ThemeID,TypeOfExerciseID,GroupID,SubjectID,Signature")] Lesson lessonF)
        {
            string teacher = _userNameService.GetDisplayName();

            if (!IsLessonDateValid(lessonF.Date, out string errorMessage))
            {
                ModelState.AddModelError("", errorMessage);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.UserName = teacher;
                ViewBag.Teachers = _context.Teachers.OrderBy(t => t.FamilyName);
                ViewBag.TeachersNoPC = _context.TeacherNoPCs.OrderBy(t => t.LastName).AsNoTracking();
                ViewBag.GroupID = GroupID;
                ViewBag.SubjectID = SubjectID;
                ViewData["ThemeID"] = new SelectList(
                    _context.Themes.Where(t => t.SubjectID == SubjectID && t.Name == "Контрольное занятие"),
                    "ThemeID", "Name");

                string[] allowedTypes =
                {
                    "Экзамен", "Дифференцированный зачёт", "Зачёт"
                };

                ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => allowedTypes.Contains(t.Name)), "TypeOfExerciseID", "Name");

                return View(lessonF);
            }

            var typeFinal = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая оценка");
            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");

            var simpleLessons = await _context.Lessons
                .Where(l => l.GroupID == GroupID && l.SubjectID == SubjectID &&
                            l.Date < lessonF.Date &&
                            l.FlagF == 0 &&
                            l.TypeOfExerciseID != typeKR.TypeOfExerciseID &&
                            l.TypeOfExerciseID != typeKP.TypeOfExerciseID)
                .ToListAsync();

            if (!simpleLessons.Any())
            {
                return RedirectToAction("ErrorF", "Lessons", new { GroupID, SubjectID });
            }

            var subject = await _context.Subjects.FindAsync(SubjectID);
            var group = await _context.Groups.FindAsync(GroupID);
            var students = await _context.Students.Where(s => s.GroupID == GroupID).ToListAsync();

            lessonF.SubjectID = SubjectID;
            lessonF.GroupID = GroupID;
            if (!User.IsInRole("ICDA-writer") && !User.IsInRole("K-8Writer"))
                lessonF.Signature = teacher;

            _context.Lessons.Add(lessonF);
            await _context.SaveChangesAsync();

            lessonF.FlagF = lessonF.LessonID;
            _context.Lessons.Update(lessonF);
            await _context.SaveChangesAsync();

            Lesson lessonFinal = new Lesson
            {
                Date = lessonF.Date.AddMinutes(10),
                FlagF = lessonF.FlagF,
                SubjectID = SubjectID,
                GroupID = GroupID,
                ThemeID = lessonF.ThemeID,
                TypeOfExerciseID = typeFinal.TypeOfExerciseID,
                Signature = (!User.IsInRole("ICDA-writer") && !User.IsInRole("K-8Writer")) ? teacher : lessonF.Signature
            };
            _context.Lessons.Add(lessonFinal);
            await _context.SaveChangesAsync();

            foreach (var lesson in simpleLessons)
            {
                lesson.FlagF = lessonF.FlagF;
                _context.Lessons.Update(lesson);
            }

            var previousMarks = await _context.Marks
                .Where(m => m.SubjectID == SubjectID && m.GroupID == GroupID && m.FlagF == 0 && m.Date < lessonF.Date)
                .ToListAsync();

            foreach (var mark in previousMarks)
            {
                mark.FlagF = lessonF.FlagF;
                _context.Marks.Update(mark);
            }

            var controlType = _context.Types.FirstOrDefault(t => t.Name == "Контрольное мероприятие");
            var controlTypeId = controlType?.TypeOfExerciseID;

            if (controlTypeId == null)
            {
                throw new InvalidOperationException("Тип 'Контрольное мероприятие' не найден в базе.");
            }

            foreach (var student in students)
            {
                var marks = new List<double>();
                var controlMarks = new List<double>();

                foreach (var mark in previousMarks)
                {
                    if (mark.StudentID == student.StudentID && double.TryParse(mark.Value, out double value))
                    {
                        marks.Add(value);

                        if (mark.TypeOfExerciseID == controlTypeId)
                        {
                            controlMarks.Add(value);
                        }
                    }
                }

                bool hasLowAverage = marks.Any() && marks.Average() < 4;
                bool hasBadControlMarks = controlMarks.Any(m => m == 1 || m == 2 || m == 3);
                bool hasMarks = !marks.Any() || !controlMarks.Any();

                if (hasLowAverage || hasBadControlMarks || hasMarks)
                {
                    _context.Marks.AddRange(
                        new Mark
                        {
                            Value = "Недопуск",
                            Date = lessonF.Date,
                            SubjectID = SubjectID,
                            GroupID = GroupID,
                            LessonID = lessonF.LessonID,
                            TypeOfExerciseID = lessonF.TypeOfExerciseID,
                            DepartmentID = subject.DepartmentID,
                            InstituteID = group.InstituteID,
                            SpecialityID = group.SpecialityID,
                            ThemeID = lessonF.ThemeID,
                            StudentID = student.StudentID,
                            FlagF = lessonF.FlagF,
                            ChangeCounter = 3
                        },
                        new Mark
                        {
                            Value = "Недопуск",
                            Date = lessonFinal.Date,
                            SubjectID = SubjectID,
                            GroupID = GroupID,
                            LessonID = lessonFinal.LessonID,
                            TypeOfExerciseID = typeFinal.TypeOfExerciseID,
                            DepartmentID = subject.DepartmentID,
                            InstituteID = group.InstituteID,
                            SpecialityID = group.SpecialityID,
                            ThemeID = lessonFinal.ThemeID,
                            StudentID = student.StudentID,
                            FlagF = lessonFinal.FlagF,
                            ChangeCounter = 3
                        });
                }
                else
                {
                    _context.Marks.AddRange(
                        new Mark
                        {
                            Value = "",
                            Date = lessonF.Date,
                            SubjectID = SubjectID,
                            GroupID = GroupID,
                            LessonID = lessonF.LessonID,
                            TypeOfExerciseID = lessonF.TypeOfExerciseID,
                            DepartmentID = subject.DepartmentID,
                            InstituteID = group.InstituteID,
                            SpecialityID = group.SpecialityID,
                            ThemeID = lessonF.ThemeID,
                            StudentID = student.StudentID,
                            FlagF = lessonF.FlagF,
                            ChangeCounter = 0
                        },
                        new Mark
                        {
                            Value = "",
                            Date = lessonFinal.Date,
                            SubjectID = SubjectID,
                            GroupID = GroupID,
                            LessonID = lessonFinal.LessonID,
                            TypeOfExerciseID = typeFinal.TypeOfExerciseID,
                            DepartmentID = subject.DepartmentID,
                            InstituteID = group.InstituteID,
                            SpecialityID = group.SpecialityID,
                            ThemeID = lessonFinal.ThemeID,
                            StudentID = student.StudentID,
                            FlagF = lessonFinal.FlagF,
                            ChangeCounter = 3
                        });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
        }

        public IActionResult Create(int? GroupID, int? SubjectID)
        {
            string teacher = _userNameService.GetDisplayName();

            var themes = _context.Themes
                .Where(t => t.SubjectID == SubjectID)
                .ToList();

            string[] allowedTypes = {
                "Семинарское занятие", "Практическое занятие",
                "Лабораторное занятие", "Лекция",
                "Контрольное мероприятие", "Городское практическое занятие"
            };

            ViewBag.Themes = themes;
            ViewData["TypeOfExerciseID"] = new SelectList(
                _context.Types.Where(t => allowedTypes.Contains(t.Name)),
                "TypeOfExerciseID", "Name"
            );

            ViewBag.UserName = teacher;
            ViewBag.Teachers = _context.Teachers.OrderBy(t => t.FamilyName);
            ViewBag.TeachersNoPC = _context.TeacherNoPCs.OrderBy(t => t.LastName).AsNoTracking();
            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int GroupID, int SubjectID, [Bind("LessonID,Date,Comment,FlagF,ThemeID,TypeOfExerciseID,GroupID,SubjectID,Signature")] Lesson lesson)
        {
            string teacher = _userNameService.GetDisplayName();

            if (!IsLessonDateValid(lesson.Date, out var errorMessage))
            {
                ModelState.AddModelError("", errorMessage);
            }

            if (!ModelState.IsValid)
            {
                var themes = await _context.Themes
                    .Where(t => t.SubjectID == SubjectID)
                    .ToListAsync();

                string[] allowedTypes = {
                    "Семинарское занятие", "Практическое занятие",
                    "Лабораторное занятие", "Лекция",
                    "Контрольное мероприятие", "Городское практическое занятие"
                };

                ViewBag.Themes = themes;
                ViewData["TypeOfExerciseID"] = new SelectList(
                    _context.Types.Where(t => allowedTypes.Contains(t.Name)),
                    "TypeOfExerciseID", "Name"
                );

                ViewBag.UserName = teacher;
                ViewBag.Teachers = _context.Teachers.OrderBy(t => t.FamilyName);
                ViewBag.TeachersNoPC = _context.TeacherNoPCs.OrderBy(t => t.LastName).AsNoTracking();
                ViewBag.GroupID = GroupID;
                ViewBag.SubjectID = SubjectID;
                return View(lesson);
            }

            lesson.SubjectID = SubjectID;
            lesson.GroupID = GroupID;

            if (!User.IsInRole("ICDA-writer") && !User.IsInRole("K-8Writer"))
            {
                lesson.Signature = teacher;
            }

            _context.Add(lesson);
            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(SubjectID);
            var group = await _context.Groups.FindAsync(GroupID);
            var students = _context.Students.Where(s => s.GroupID == GroupID);

            foreach (var student in students)
            {
                var mark = new Mark
                {
                    Value = "",
                    Date = lesson.Date,
                    SubjectID = SubjectID,
                    GroupID = GroupID,
                    LessonID = lesson.LessonID,
                    TypeOfExerciseID = lesson.TypeOfExerciseID,
                    DepartmentID = subject.DepartmentID,
                    InstituteID = group.InstituteID,
                    SpecialityID = group.SpecialityID,
                    ThemeID = lesson.ThemeID,
                    StudentID = student.StudentID
                };
                _context.Marks.Add(mark);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
        }

        public IActionResult CreateK(int? GroupID, int? SubjectID)
        {

            string teacher = _userNameService.GetDisplayName();

            ViewData["ThemeID"] = new SelectList(_context.Themes.Where(t => t.SubjectID == SubjectID && t.Name == "Контрольное занятие"), "ThemeID", "Name");
            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Курсовая работа" || t.Name == "Курсовой проект"), "TypeOfExerciseID", "Name");
            var teachers = _context.Teachers.OrderBy(t => t.FamilyName);
            var teachersNoPC = _context.TeacherNoPCs.OrderBy(t => t.LastName).AsNoTracking();
            ViewBag.UserName = teacher;
            ViewBag.TeachersNoPC = teachersNoPC;
            ViewBag.Teachers = teachers;
            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateK(int GroupID, int SubjectID, [Bind("LessonID,Date,Comment,FlagX,FlagF,ThemeID,TypeOfExerciseID,GroupID,SubjectID,Signature")] Lesson lessonK)
        {
            string teacher = _userNameService.GetDisplayName();

            if (!IsLessonDateValid(lessonK.Date, out var errorMessage))
            {
                ModelState.AddModelError("", errorMessage);
            }

            if (!ModelState.IsValid)
            {
                ViewData["ThemeID"] = new SelectList(
                    _context.Themes.Where(t => t.SubjectID == SubjectID && t.Name == "Контрольное занятие"),
                    "ThemeID", "Name");

                ViewData["TypeOfExerciseID"] = new SelectList(
                    _context.Types.Where(t => t.Name == "Курсовая работа" || t.Name == "Курсовой проект"),
                    "TypeOfExerciseID", "Name");

                var teachers = _context.Teachers.OrderBy(t => t.FamilyName);
                var teachersNoPC = _context.TeacherNoPCs.OrderBy(t => t.LastName).AsNoTracking();
                ViewBag.UserName = teacher;
                ViewBag.TeachersNoPC = teachersNoPC;
                ViewBag.Teachers = teachers;
                ViewBag.GroupID = GroupID;
                ViewBag.SubjectID = SubjectID;

                return View(lessonK);
            }

            lessonK.SubjectID = SubjectID;
            lessonK.GroupID = GroupID;
            if (!User.IsInRole("ICDA-writer") && !User.IsInRole("K-8Writer"))
            {
                lessonK.Signature = teacher;
            }

            _context.Add(lessonK);
            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(SubjectID);
            var group = await _context.Groups.FindAsync(GroupID);
            var students = await _context.Students.Where(s => s.GroupID == GroupID).ToListAsync();

            foreach (var student in students)
            {
                Mark mark = new()
                {
                    Value = "",
                    Date = lessonK.Date,
                    SubjectID = SubjectID,
                    GroupID = GroupID,
                    LessonID = lessonK.LessonID,
                    TypeOfExerciseID = lessonK.TypeOfExerciseID,
                    DepartmentID = subject.DepartmentID,
                    InstituteID = group.InstituteID,
                    SpecialityID = group.SpecialityID,
                    ThemeID = lessonK.ThemeID,
                    StudentID = student.StudentID
                };

                _context.Marks.Add(mark);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
        }

        public async Task<IActionResult> Delete(int? id, int? GroupID, int? SubjectID)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Group)
                .Include(l => l.Theme)
                    .ThenInclude(t => t.Subject)
                .Include(l => l.TypeOfExercise)
                .FirstOrDefaultAsync(m => m.LessonID == id);
            if (lesson == null)
            {
                return NotFound();
            }
            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;
            return View(lesson);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int GroupID, int SubjectID)
        {
            string teacher = _userNameService.GetDisplayName(); ;

            var lesson = await _context.Lessons.FindAsync(id);
            var marks = _context.Marks.Where(m => m.LessonID == id);
            foreach (var mark in marks)
            {
                _context.Marks.Remove(mark);
            }
            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(lesson.SubjectID);
            var theme = await _context.Themes.FindAsync(lesson.ThemeID);
            var type = await _context.Types.FindAsync(lesson.TypeOfExerciseID);
            var group = await _context.Groups.FindAsync(GroupID);
            Event e = new();
            e.Date = DateTime.Now;
            e.Teacher = teacher;
            e.Log = "Удалено занятие от: " + lesson.Date.ToShortDateString() + ", предмет: " + subject.Name + " , тема: " + theme.Name + ", тип: " + type.Name + ", группа: " + group.Name;
            _context.Events.Update(e);
            await _context.SaveChangesAsync();

            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
        }

        public async Task<IActionResult> DeleteF(int? id, int? GroupID, int? SubjectID)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Group)
                .Include(l => l.Theme)
                    .ThenInclude(t => t.Subject)
                .Include(l => l.TypeOfExercise)
                .FirstOrDefaultAsync(m => m.LessonID == id);
            if (lesson == null)
            {
                return NotFound();
            }
            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;
            return View(lesson);
        }

        [HttpPost, ActionName("DeleteF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFConfirmed(int id, int GroupID, int SubjectID)
        {
            string teacher = _userNameService.GetDisplayName(); ;

            var typeOfExerciseIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая оценка");

            var lesson = await _context.Lessons.FindAsync(id);
            var marks = _context.Marks.Where(m => m.LessonID == id);
            foreach (var mark in marks)
            {
                _context.Marks.Remove(mark);
            }
            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            var lessonIO = await _context.Lessons.FirstOrDefaultAsync(l => l.FlagF == lesson.FlagF && l.TypeOfExerciseID == typeOfExerciseIO.TypeOfExerciseID);
            var marksIO = _context.Marks.Where(m => m.LessonID == lessonIO.LessonID);
            foreach (var mark in marksIO)
            {
                _context.Marks.Remove(mark);
            }
            _context.Lessons.Remove(lessonIO);
            await _context.SaveChangesAsync();

            var lessons = _context.Lessons.Where(l => l.FlagF == lesson.FlagF);
            foreach (var item in lessons)
            {
                item.FlagF = 0;
                _context.Lessons.Update(item);
            }
            await _context.SaveChangesAsync();

            var simpleMarks = _context.Marks.Where(l => l.FlagF == lesson.FlagF);
            foreach (var mark in simpleMarks)
            {
                mark.FlagF = 0;
                _context.Marks.Update(mark);
            }
            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(lesson.SubjectID);
            var theme = await _context.Themes.FindAsync(lesson.ThemeID);
            var type = await _context.Types.FindAsync(lesson.TypeOfExerciseID);
            var group = await _context.Groups.FindAsync(GroupID);
            Event e = new();
            e.Date = DateTime.Now;
            e.Teacher = teacher;
            e.Log = "Удалено занятие от: " + lesson.Date.ToShortDateString() + ", предмет: " + subject.Name + " , тема: " + theme.Name + ", тип: " + type.Name + ", группа: " + group.Name;
            _context.Events.Update(e);
            await _context.SaveChangesAsync();
            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
        }

        public IActionResult Error(int GroupID, int SubjectID)
        {
            ViewBag.SubjectID = SubjectID;
            ViewBag.GroupID = GroupID;
            return View();
        }

        public IActionResult ErrorF(int GroupID, int SubjectID)
        {
            ViewBag.SubjectID = SubjectID;
            ViewBag.GroupID = GroupID;
            return View();
        }

        private bool IsLessonDateValid(DateTime lessonDate, out string errorMessage)
        {
            var today = DateTime.Today;
            var currentYear = today.Year;
            var currentMonth = today.Month;
            var currentDay = today.Day;

            var lessonYear = lessonDate.Year;
            var lessonMonth = lessonDate.Month;

            if (lessonDate > DateTime.Now)
            {
                errorMessage = "Невозможно создать занятие в будущем!";
                return false;
            }

            bool isLessonInPreviousMonth =
                (lessonYear == currentYear && lessonMonth == currentMonth - 1) ||
                (currentMonth == 1 && lessonYear == currentYear - 1 && lessonMonth == 12);

            if (isLessonInPreviousMonth && currentDay > 9)
            {
                errorMessage = "Создание занятий в прошлом месяце возможно только до 10 числа текущего месяца.";
                return false;
            }

            var startOfCurrentMonth = new DateTime(currentYear, currentMonth, 1);
            if (lessonDate < startOfCurrentMonth && !isLessonInPreviousMonth)
            {
                errorMessage = "Невозможно создать занятие в более ранних месяцах.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private bool LessonExists(int id)
        {
            return _context.Lessons.Any(e => e.LessonID == id);
        }
    }
}