using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class MarksController : Controller
    {
        private readonly AcademyContext _context;
        private readonly UserNameService _userNameService;

        public MarksController(AcademyContext context, UserNameService userNameService)
        {
            _context = context;
            _userNameService = userNameService;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index(DateTime date)
        {
            var marks = _context.Marks
                .Where(l => l.Date.Year == DateTime.Now.Year && l.Date.Month == DateTime.Now.Month && l.Date.Day == DateTime.Now.Day && l.Value != "")
                .Include(l => l.Theme)
                    .ThenInclude(t => t.Subject)
                .Include(l => l.Student)
                    .ThenInclude(s => s.Group);

            if (date.ToShortDateString() != "01.01.0001")
            {
                marks = _context.Marks
                 .Where(l => l.Date.Year == date.Year && l.Date.Month == date.Month && l.Date.Day == date.Day && l.Value != "")
                  .Include(l => l.Theme)
                    .ThenInclude(t => t.Subject)
                .Include(l => l.Student)
                    .ThenInclude(s => s.Group);
            }

            return View(await marks.ToListAsync());
        }

        public async Task<IActionResult> Edit(int? MarkID, int? GroupID, int? SubjectID)
        {
            if (MarkID == null)
            {
                return NotFound();
            }

            string teacher = _userNameService.GetDisplayName();

            var mark = await _context.Marks.FindAsync(MarkID);

            if (mark == null)
            {
                return NotFound();
            }
            var teachers = _context.Teachers.OrderBy(t => t.FamilyName).AsNoTracking();
            var teachersNoPC = _context.TeacherNoPCs.OrderBy(t => t.LastName).AsNoTracking();
            ViewBag.UserName = teacher;
            ViewBag.TeachersNoPC = teachersNoPC;
            ViewBag.Teachers = teachers;
            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;
            return View(mark);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int MarkID, int? GroupID, int? SubjectID, [Bind("MarkID,Value,Date,Comment,SignatureOfTeacher,HistoryOfMark,SubjectID,GroupID,LessonID,TypeOfExerciseID,DepartmentID,SpecialityID,ThemeID,StudentID,FlagX,FlagF,InstituteID")] Mark mark)
        {
            if (MarkID != mark.MarkID)
            {
                return NotFound();
            }

            string teacher = _userNameService.GetDisplayName();

            if (mark.HistoryOfMark != null)
            {
                var subject = await _context.Subjects.FindAsync(mark.SubjectID);
                var theme = await _context.Themes.FindAsync(mark.ThemeID);
                var type = await _context.Types.FindAsync(mark.TypeOfExerciseID);
                var student = await _context.Students.FindAsync(mark.StudentID);
                var group = await _context.Groups.FindAsync(mark.GroupID);

                Event e = new();
                e.Date = DateTime.Now;
                if (User.IsInRole("ICDA-writer") || User.IsInRole("K-8Writer"))
                {
                    e.Teacher = mark.SignatureOfTeacher;
                }
                else
                {
                    e.Teacher = teacher;
                }
                e.Log = "Изменение оценки от " + mark.Date.ToShortDateString() + ", предмет: " + subject.Name + ", тема: " + theme.Name + ", тип занятия: " + type.Name + ", курсант/слушатель: "
                    + student.LastName + " " + student.Name[0] + "." + student.Surname[0] + "." + ", группа: " + group.Name;
                _context.Events.Update(e);
                await _context.SaveChangesAsync();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (User.IsInRole("ICDA-writer") || User.IsInRole("K-8Writer"))
                    {
                        mark.HistoryOfMark += mark.Value + " - " + DateTime.Now.ToShortDateString() + " - " + mark.SignatureOfTeacher + "</br>";
                        _context.Marks.Update(mark);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        mark.SignatureOfTeacher = teacher;
                        mark.HistoryOfMark += mark.Value + " - " + DateTime.Now.ToShortDateString() + " - " + teacher + "</br>";
                        _context.Marks.Update(mark);
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    if (!MarkExists(mark.MarkID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
            }
            return View(mark);
        }

        private bool MarkExists(int id)
        {
            return _context.Marks.Any(e => e.MarkID == id);
        }
    }
}
