using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;

namespace Portal
{
    public class StatementMarksController : Controller
    {
        private readonly AcademyContext _context;

        public StatementMarksController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index(DateTime date)
        {
            var types = _context.Types;
            var marks = _context.StatementMarks
                .Where(l => l.Date.Year == DateTime.Now.Year && l.Date.Month == DateTime.Now.Month && l.Date.Day == DateTime.Now.Day && l.Value != "")
                .Include(l => l.Student)
                    .ThenInclude(s => s.Group);

            if (date.ToShortDateString() != "01.01.0001")
            {
                marks = _context.StatementMarks
                .Where(l => l.Date.Year == date.Year && l.Date.Month == date.Month && l.Date.Day == date.Day && l.Value != "")
                .Include(l => l.Student)
                    .ThenInclude(s => s.Group);
            }
            ViewBag.Types = types;
            return View(await marks.ToListAsync());
        }

        public async Task<IActionResult> Edit(int? id, int? GroupID)
        {
            if (id == null)
            {
                return NotFound();
            }

            var statementMark = await _context.StatementMarks.FindAsync(id);
            if (statementMark == null)
            {
                return NotFound();
            }
            ViewBag.GroupID = GroupID;
            return View(statementMark);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int GroupID, [Bind("StatementMarkID,Value,Date,Comment,SignatureOfTeacher,HistoryOfMark,InstituteID,SpecialityID,GroupID,TypeOfExerciseID,StatementLessonID,StudentID")] StatementMark statementMark)
        {
            if (id != statementMark.StatementMarkID)
            {
                return NotFound();
            }

            if (statementMark.HistoryOfMark != null)
            {
                var type = await _context.Types.FindAsync(statementMark.TypeOfExerciseID);
                var student = await _context.Students.FindAsync(statementMark.StudentID);
                var group = await _context.Groups.FindAsync(statementMark.GroupID);

                Event e = new();
                e.Date = statementMark.Date;
                e.Teacher = User.Identity.Name;
                e.Log = "Изменение оценки от " + statementMark.Date.ToShortDateString() + ", тип занятия: " + type.Name + ", курсант/слушатель: "
                    + student.LastName + " " + student.Name[0] + "." + student.Surname[0] + "." + ", группа: " + group.Name;
                _context.Events.Update(e);
            }
            await _context.SaveChangesAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    statementMark.SignatureOfTeacher = User.Identity.Name;
                    statementMark.HistoryOfMark += statementMark.Value + " - " + statementMark.Date.ToShortDateString() + " - " + User.Identity.Name + "</br>";
                    _context.StatementMarks.Update(statementMark);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StatementMarkExists(statementMark.StatementMarkID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Statement", "Journals", new { GroupID });
            }
            ViewBag.GroupID = GroupID;
            return View(statementMark);
        }
        private bool StatementMarkExists(int id)
        {
            return _context.StatementMarks.Any(e => e.StatementMarkID == id);
        }
    }
}