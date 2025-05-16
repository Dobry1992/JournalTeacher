using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                return NotFound();

            var mark = await _context.Marks.FindAsync(MarkID);
            if (mark == null)
                return NotFound();

            string teacher = _userNameService.GetDisplayName();

            ViewBag.UserName = teacher;
            ViewBag.Teachers = _context.Teachers.OrderBy(t => t.FamilyName).AsNoTracking();
            ViewBag.TeachersNoPC = _context.TeacherNoPCs.OrderBy(t => t.LastName).AsNoTracking();
            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;

            return View(mark);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int MarkID, int? GroupID, int? SubjectID,[Bind("MarkID,Value,Date,Comment,SignatureOfTeacher,HistoryOfMark,SubjectID,GroupID,LessonID,TypeOfExerciseID,DepartmentID,SpecialityID,ThemeID,StudentID,FlagX,FlagF,InstituteID,ChangeCounter")] Mark mark)
        {
            if (MarkID != mark.MarkID)
                return NotFound();

            if (!ModelState.IsValid)
                return View(mark);

            string teacher = _userNameService.GetDisplayName();
            bool isWriter = User.IsInRole("ICDA-writer") || User.IsInRole("K-8Writer");

            if (!isWriter)
                mark.SignatureOfTeacher = teacher;

            if (!string.IsNullOrWhiteSpace(mark.HistoryOfMark))
            {
                var subject = await _context.Subjects.FindAsync(mark.SubjectID);
                var theme = await _context.Themes.FindAsync(mark.ThemeID);
                var type = await _context.Types.FindAsync(mark.TypeOfExerciseID);
                var student = await _context.Students.FindAsync(mark.StudentID);
                var group = await _context.Groups.FindAsync(mark.GroupID);

                if (subject != null && theme != null && type != null && student != null && group != null)
                {
                    var e = new Event
                    {
                        Date = DateTime.Now,
                        Teacher = mark.SignatureOfTeacher,
                        Log = $"Изменение оценки от {mark.Date:dd.MM.yyyy}, предмет: {subject.Name}, тема: {theme.Name}, тип занятия: {type.Name}, курсант/слушатель: {student.LastName} {student.Name[0]}.{student.Surname[0]}. группа: {group.Name}"
                    };
                    _context.Events.Add(e);
                    await _context.SaveChangesAsync();
                }
            }

            mark.HistoryOfMark = (mark.HistoryOfMark ?? string.Empty) +
                                 $"{mark.Value} - {DateTime.Now:dd.MM.yyyy} - {mark.SignatureOfTeacher}</br>";
            mark.ChangeCounter++;
            _context.Marks.Update(mark);
            await _context.SaveChangesAsync();

            try
            {
                if (mark.FlagF != 0)
                {
                    var types = await _context.Types.ToListAsync();
                    var typeZachet = types.FirstOrDefault(t => t.Name == "Зачёт");
                    var typeDiffZachet = types.FirstOrDefault(t => t.Name == "Дифференцированный зачёт");
                    var typeExam = types.FirstOrDefault(t => t.Name == "Экзамен");
                    var typeItog = types.FirstOrDefault(t => t.Name == "Итоговая оценка");
                    var typeKontrol = types.FirstOrDefault(t => t.Name == "Контрольное мероприятие");

                    if (typeZachet == null || typeDiffZachet == null || typeExam == null || typeItog == null || typeKontrol == null)
                        throw new InvalidOperationException("Не все типы занятий найдены в справочнике.");

                    var requiredTypeIds = new List<int>
                    {
                        typeZachet.TypeOfExerciseID,
                        typeDiffZachet.TypeOfExerciseID,
                        typeExam.TypeOfExerciseID
                    };

                    var markIA = await _context.Marks.FirstOrDefaultAsync(m =>
                        m.FlagF == mark.FlagF &&
                        requiredTypeIds.Contains(m.TypeOfExerciseID) &&
                        m.StudentID == mark.StudentID);

                    var markIO = await _context.Marks.FirstOrDefaultAsync(m =>
                        m.FlagF == mark.FlagF &&
                        m.TypeOfExerciseID == typeItog.TypeOfExerciseID &&
                        m.StudentID == mark.StudentID);

                    if (markIA == null || markIO == null)
                        return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });

                    var marks = await _context.Marks
                        .Where(m => m.FlagF == mark.FlagF &&
                                    m.StudentID == mark.StudentID &&
                                    m.TypeOfExerciseID != markIA.TypeOfExerciseID &&
                                    m.TypeOfExerciseID != markIO.TypeOfExerciseID)
                        .ToListAsync();

                    List<double> doubleMarks = new();
                    List<double> doubleControlMarks = new();

                    foreach (var m in marks)
                    {
                        if (TryParseMarkValue(m.Value, out double number))
                        {
                            doubleMarks.Add(number);
                            if (m.TypeOfExerciseID == typeKontrol.TypeOfExerciseID)
                                doubleControlMarks.Add(number);
                        }
                    }

                    if (mark.TypeOfExerciseID == markIA.TypeOfExerciseID)
                    {
                        bool updatedIO = false;

                        if (markIA.TypeOfExerciseID == typeZachet.TypeOfExerciseID)
                        {
                            if (mark.Value == "З")
                            {
                                markIO.Value = "Зачтено";
                                updatedIO = true;
                            }
                            else if (mark.Value == "НЗ")
                            {
                                markIO.Value = "Не зачтено";
                                updatedIO = true;
                            }
                        }
                        else
                        {
                            if (new[] { "1", "2", "3" }.Contains(markIA.Value))
                            {
                                markIO.Value = markIA.Value;
                                updatedIO = true;
                            }
                            else if (TryParseMarkValue(markIA.Value, out double num) && doubleMarks.Any())
                            {
                                double average = doubleMarks.Average();
                                double finalValue = average * 0.6 + num * 0.4;
                                markIO.Value = Math.Round(finalValue).ToString(CultureInfo.InvariantCulture);
                                updatedIO = true;
                            }
                        }

                        if (updatedIO)
                        {
                            markIO.ChangeCounter = 3;
                            _context.Marks.Update(markIO);
                        }
                    }

                    bool hasLowControlMark = doubleControlMarks.Any(x => x <= 3);
                    bool noControlMarks = !doubleControlMarks.Any();
                    bool lowAverage = doubleMarks.Any() && doubleMarks.Average() < 4;

                    if (lowAverage || hasLowControlMark || noControlMarks)
                    {
                        markIA.Value = "Недопуск";
                        markIO.Value = "Недопуск";
                        markIA.ChangeCounter = 3;
                        markIO.ChangeCounter = 3;
                        _context.Marks.Update(markIA);
                        _context.Marks.Update(markIO);
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MarkExists(mark.MarkID))
                    return NotFound();
                throw;
            }

            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
        }

        private bool TryParseMarkValue(string value, out double number)
        {
            number = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var nonNumericValues = new[] { "З", "НЗ", "Зачтено", "Не зачтено", "Недопуск" };
            if (nonNumericValues.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
                return false;

            return double.TryParse(value, NumberStyles.Any, new CultureInfo("ru-RU"), out number);
        }

        private bool MarkExists(int id)
        {
            return _context.Marks.Any(e => e.MarkID == id);
        }
    }
}
