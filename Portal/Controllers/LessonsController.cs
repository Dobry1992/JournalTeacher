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

namespace Portal
{
    public class LessonsController : Controller
    {
        private readonly AcademyContext _context;

        public LessonsController(AcademyContext context)
        {
            _context = context;
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
                    .Where(l => l.Date.Year >= date_1.Date.Year && l.Date.Month >= date_1.Date.Month && l.Date.Day >= date_1.Day)
                    .ToList();
            }

            if (date_2.ToShortDateString() != "01.01.0001")
            {
                lessons = lessons
                    .Where(l => l.Date.Year <= date_2.Date.Year && l.Date.Month <= date_2.Date.Month && l.Date.Day <= date_2.Day)
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

            string teacher = "";
            var username = User.Identity.Name;
            using (var context = new PrincipalContext(ContextType.Domain, AD.root))
            {
                try
                {
                    var user = UserPrincipal.FindByIdentity(context, username);

                    if (user != null)
                    {
                        teacher = user.DisplayName;
                    }
                }
                catch
                {
                    teacher = username;
                }
            }

            ViewData["ThemeID"] = new SelectList(_context.Themes.Where(t => t.SubjectID == SubjectID && t.Name == "Контрольное занятие"), "ThemeID", "Name");
            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Экзамен" || t.Name == "Дифференцированный зачёт" || t.Name == "Зачёт"), "TypeOfExerciseID", "Name");
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
        public async Task<IActionResult> CreateF(int GroupID, int SubjectID, [Bind("LessonID,Date,Comment,FlagF,ThemeID,TypeOfExerciseID,GroupID,SubjectID,Signature")] Lesson lessonF)
        {
            if (lessonF.Date > DateTime.Now)
            {
                ModelState.AddModelError("", "Невозможно создать занятие в будущем!");
            }

            if (ModelState.IsValid)
            {
                var typeOfExerciseIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая оценка");
                var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
                var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
                var simpleLessons = _context.Lessons.Where(l => l.GroupID == GroupID && l.SubjectID == SubjectID && l.Date < lessonF.Date && l.FlagF == 0 && l.TypeOfExerciseID != typeKR.TypeOfExerciseID && l.TypeOfExerciseID != typeKP.TypeOfExerciseID);
                var subject = await _context.Subjects.FindAsync(SubjectID);
                var group = await _context.Groups.FindAsync(GroupID);
                var students = _context.Students.Where(s => s.GroupID == GroupID);

                string teacher = "";
                var username = User.Identity.Name;
                using (var context = new PrincipalContext(ContextType.Domain, AD.root))
                {
                    try
                    {
                        var user = UserPrincipal.FindByIdentity(context, username);

                        if (user != null)
                        {
                            teacher = user.DisplayName;
                        }
                    }
                    catch
                    {
                        teacher = username;
                    }
                }

                if (!simpleLessons.Any())
                {
                    return RedirectToAction("ErrorF", "Lessons", new { GroupID, SubjectID });
                }
                else
                {
                    lessonF.SubjectID = SubjectID;
                    lessonF.GroupID = GroupID;
                    if (!User.IsInRole("ICDA-writer") && !User.IsInRole("K-8Writer"))
                    {
                        lessonF.Signature = teacher;
                    }
                    _context.Lessons.Add(lessonF);
                    await _context.SaveChangesAsync();
                    lessonF.FlagF = lessonF.LessonID;
                    _context.Lessons.Update(lessonF);
                    await _context.SaveChangesAsync();

                    Lesson lessonIO = new();
                    lessonIO.Date = lessonF.Date.AddMinutes(10);
                    lessonIO.FlagF = lessonF.FlagF;
                    lessonIO.SubjectID = SubjectID;
                    lessonIO.GroupID = GroupID;
                    lessonIO.ThemeID = lessonF.ThemeID;
                    lessonIO.TypeOfExerciseID = typeOfExerciseIO.TypeOfExerciseID;
                    if (!User.IsInRole("ICDA-writer") && !User.IsInRole("K-8Writer"))
                    {
                        lessonIO.Signature = teacher;
                    }
                    else
                    {
                        lessonIO.Signature = lessonF.Signature;
                    }
                    _context.Lessons.Add(lessonIO);
                    await _context.SaveChangesAsync();

                    foreach (var lesson in simpleLessons)
                    {
                        lesson.FlagF = lessonF.FlagF;
                        _context.Lessons.Update(lesson);
                    }
                    await _context.SaveChangesAsync();

                    var marks = _context.Marks.Where(m => m.SubjectID == SubjectID && m.GroupID == GroupID && m.FlagF == 0 && m.Date < lessonF.Date);
                    foreach (var mark in marks)
                    {
                        mark.FlagF = lessonF.FlagF;
                        _context.Marks.Update(mark);
                    }
                    await _context.SaveChangesAsync();

                    foreach (var student in students)
                    {
                        Mark markF = new();
                        markF.Value = "";
                        markF.Date = lessonF.Date;
                        markF.SubjectID = SubjectID;
                        markF.GroupID = GroupID;
                        markF.LessonID = lessonF.LessonID;
                        markF.TypeOfExerciseID = lessonF.TypeOfExerciseID;
                        markF.DepartmentID = subject.DepartmentID;
                        markF.InstituteID = group.InstituteID;
                        markF.SpecialityID = group.SpecialityID;
                        markF.ThemeID = lessonF.ThemeID;
                        markF.StudentID = student.StudentID;
                        markF.FlagF = lessonF.FlagF;
                        _context.Marks.Add(markF);

                        Mark markIO = new();
                        markIO.Value = "";
                        markIO.Date = lessonIO.Date;
                        markIO.SubjectID = SubjectID;
                        markIO.GroupID = GroupID;
                        markIO.LessonID = lessonIO.LessonID;
                        markIO.TypeOfExerciseID = typeOfExerciseIO.TypeOfExerciseID;
                        markIO.DepartmentID = subject.DepartmentID;
                        markIO.InstituteID = group.InstituteID;
                        markIO.SpecialityID = group.SpecialityID;
                        markIO.ThemeID = lessonIO.ThemeID;
                        markIO.StudentID = student.StudentID;
                        markIO.FlagF = lessonIO.FlagF;
                        _context.Marks.Add(markIO);
                    }
                }
                await _context.SaveChangesAsync();

                return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
            }
            ViewData["ThemeID"] = new SelectList(_context.Themes.Where(t => t.SubjectID == SubjectID && t.Name == "Контрольное занятие"), "ThemeID", "Name");
            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Экзамен" || t.Name == "Дифференцированный зачёт" || t.Name == "Зачёт"), "TypeOfExerciseID", "Name");
            return View(lessonF);
        }

        public IActionResult Create(int? GroupID, int? SubjectID)
        {
            string teacher = "";
            var username = User.Identity.Name;
            using (var context = new PrincipalContext(ContextType.Domain, AD.root))
            {
                try
                {
                    var user = UserPrincipal.FindByIdentity(context, username);

                    if (user != null)
                    {
                        teacher = user.DisplayName;
                    }
                }
                catch
                {
                    teacher = username;
                }
            }
            List<Theme> themes = new();
            themes = _context.Themes.Where(t => t.SubjectID == SubjectID && t.Name != "Контрольное занятие").ToList();
            ViewBag.Themes = themes;
            
            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Семинарское занятие" || t.Name == "Практическое занятие"
                || t.Name == "Лабораторное занятие" || t.Name == "Лекция" || t.Name == "Контрольное мероприятие" || t.Name == "Городское практическое занятие"), "TypeOfExerciseID", "Name");
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
        public async Task<IActionResult> Create(int GroupID, int SubjectID, [Bind("LessonID,Date,Comment,FlagF,ThemeID,TypeOfExerciseID,GroupID,SubjectID,Signature")] Lesson lesson)
        {
            if (lesson.Date > DateTime.Now)
            {
                ModelState.AddModelError("", "Невозможно создать занятие в будущем!");
            }

            if (ModelState.IsValid)
            {
                string teacher = "";
                var username = User.Identity.Name;
                using (var context = new PrincipalContext(ContextType.Domain, AD.root))
                {
                    try
                    {
                        var user = UserPrincipal.FindByIdentity(context, username);

                        if (user != null)
                        {
                            teacher = user.DisplayName;
                        }
                    }
                    catch
                    {
                        teacher = username;
                    }
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
                    Mark mark = new();
                    mark.Value = "";
                    mark.Date = lesson.Date;
                    mark.SubjectID = SubjectID;
                    mark.GroupID = GroupID;
                    mark.LessonID = lesson.LessonID;
                    mark.TypeOfExerciseID = lesson.TypeOfExerciseID;
                    mark.DepartmentID = subject.DepartmentID;
                    mark.InstituteID = group.InstituteID;
                    mark.SpecialityID = group.SpecialityID;
                    mark.ThemeID = lesson.ThemeID;
                    mark.StudentID = student.StudentID;
                    _context.Marks.Add(mark);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
            }

            List<Theme> themes = new();
            themes = await _context.Themes.Where(t => t.SubjectID == SubjectID && t.Name != "Контрольное занятие").ToListAsync();
            ViewBag.Themes = themes;
            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Семинарское занятие" || t.Name == "Практическое занятие"
                || t.Name == "Лабораторное занятие" || t.Name == "Лекция" || t.Name == "Контрольное мероприятие" || t.Name == "Городское практическое занятие"), "TypeOfExerciseID", "Name");
            return View(lesson);
        }

        public IActionResult CreateK(int? GroupID, int? SubjectID)
        {
            string teacher = "";
            var username = User.Identity.Name;
            using (var context = new PrincipalContext(ContextType.Domain, AD.root))
            {
                try
                {
                    var user = UserPrincipal.FindByIdentity(context, username);

                    if (user != null)
                    {
                        teacher = user.DisplayName;
                    }
                }
                catch
                {
                    teacher = username;
                }
            }
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
            if (lessonK.Date > DateTime.Now)
            {
                ModelState.AddModelError("", "Невозможно создать занятие в будущем!");
            }

            if (ModelState.IsValid)
            {
                string teacher = "";
                var username = User.Identity.Name;
                using (var context = new PrincipalContext(ContextType.Domain, AD.root))
                {
                    try
                    {
                        var user = UserPrincipal.FindByIdentity(context, username);

                        if (user != null)
                        {
                            teacher = user.DisplayName;
                        }
                    }
                    catch
                    {
                        teacher = username;
                    }
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
                var students = _context.Students.Where(s => s.GroupID == GroupID);

                foreach (var student in students)
                {
                    Mark mark = new();
                    mark.Value = "";
                    mark.Date = lessonK.Date;
                    mark.SubjectID = SubjectID;
                    mark.GroupID = GroupID;
                    mark.LessonID = lessonK.LessonID;
                    mark.TypeOfExerciseID = lessonK.TypeOfExerciseID;
                    mark.DepartmentID = subject.DepartmentID;
                    mark.InstituteID = group.InstituteID;
                    mark.SpecialityID = group.SpecialityID;
                    mark.ThemeID = lessonK.ThemeID;
                    mark.StudentID = student.StudentID;
                    _context.Marks.Add(mark);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
            }
            ViewData["ThemeID"] = new SelectList(_context.Themes.Where(t => t.SubjectID == SubjectID && t.Name == "Контрольное занятие"), "ThemeID", "Name");
            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Курсовая работа" || t.Name == "Курсовой проект"), "TypeOfExerciseID", "Name");
            return View(lessonK);
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
            string teacher = "";
            var username = User.Identity.Name;
            using (var context = new PrincipalContext(ContextType.Domain, AD.root))
            {
                try
                {
                    var user = UserPrincipal.FindByIdentity(context, username);

                    if (user != null)
                    {
                        teacher = user.DisplayName;
                    }
                }
                catch
                {
                    teacher = username;
                }
            }

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
            string teacher = "";
            var username = User.Identity.Name;
            using (var context = new PrincipalContext(ContextType.Domain, AD.root))
            {
                try
                {
                    var user = UserPrincipal.FindByIdentity(context, username);

                    if (user != null)
                    {
                        teacher = user.DisplayName;
                    }
                }
                catch
                {
                    teacher = username;
                }
            }

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

        private bool LessonExists(int id)
        {
            return _context.Lessons.Any(e => e.LessonID == id);
        }
    }
}