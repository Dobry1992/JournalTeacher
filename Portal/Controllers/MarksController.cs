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
        public async Task<IActionResult> Edit(int MarkID, int? GroupID, int? SubjectID, [Bind("MarkID,Value,Date,Comment,SignatureOfTeacher,HistoryOfMark,SubjectID,GroupID,LessonID,TypeOfExerciseID,DepartmentID,SpecialityID,ThemeID,StudentID,FlagX,FlagF,InstituteID,ChangeCounter")] Mark mark)
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
                        Log = $"Изменение отметки от {mark.Date:dd.MM.yyyy}, предмет: {subject.Name}, тема: {theme.Name}, тип занятия: {type.Name}, курсант/слушатель: {student.LastName} {student.Name[0]}.{student.Surname[0]}. группа: {group.Name}"
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
                    var typeItog = types.FirstOrDefault(t => t.Name == "Итоговая отметка");
                    var typeKontrol = types.FirstOrDefault(t => t.Name == "Контрольное мероприятие");

                    if (typeZachet == null || typeDiffZachet == null || typeExam == null || typeItog == null || typeKontrol == null)
                        throw new InvalidOperationException("Не все типы занятий найдены в справочнике.");

                    var requiredTypeIds = new List<int>
                    {
                        typeZachet.TypeOfExerciseID,
                        typeDiffZachet.TypeOfExerciseID,
                        typeExam.TypeOfExerciseID
                    };

                    var IALessons = await _context.Lessons
                        .Where(l =>
                            l.GroupID == mark.GroupID &&
                            l.FlagF == mark.FlagF &&
                            l.SubjectID == mark.SubjectID &&
                            (l.TypeOfExerciseID == typeExam.TypeOfExerciseID || l.TypeOfExerciseID == typeZachet.TypeOfExerciseID))
                        .ToListAsync();

                    if (IALessons.Count > 1)
                    {
                        var student = await _context.Students.FindAsync(mark.StudentID);
                        var lessonExam = IALessons.FirstOrDefault(l => l.TypeOfExerciseID == typeExam.TypeOfExerciseID);
                        var lessonZ = IALessons.FirstOrDefault(l => l.TypeOfExerciseID == typeZachet.TypeOfExerciseID);
                        var lessonIOExam = await _context.Lessons.FirstOrDefaultAsync(l =>
                            l.FlagF == mark.FlagF &&
                            l.TypeOfExerciseID == typeItog.TypeOfExerciseID &&
                            l.SubjectID == SubjectID &&
                            l.GroupID == GroupID &&
                            l.Date.Year == lessonExam.Date.Year &&
                            l.Date.Month == lessonExam.Date.Month &&
                            l.Date.Day == lessonExam.Date.Day);
                        var lessonIOZ = await _context.Lessons.FirstOrDefaultAsync(l =>
                            l.FlagF == mark.FlagF &&
                            l.TypeOfExerciseID == typeItog.TypeOfExerciseID &&
                            l.SubjectID == SubjectID &&
                            l.GroupID == GroupID &&
                            l.Date.Year == lessonZ.Date.Year &&
                            l.Date.Month == lessonZ.Date.Month &&
                            l.Date.Day == lessonZ.Date.Day);

                        if (lessonExam == null || lessonZ == null || lessonIOExam == null || lessonIOZ == null)
                        {
                            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
                        }

                        var markExam = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.StudentID == mark.StudentID &&
                            m.LessonID == lessonExam.LessonID &&
                            m.TypeOfExerciseID == typeExam.TypeOfExerciseID);
                        var markZ = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.StudentID == mark.StudentID &&
                            m.LessonID == lessonZ.LessonID &&
                            m.TypeOfExerciseID == typeZachet.TypeOfExerciseID);
                        var markIOExam = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.StudentID == mark.StudentID &&
                            m.LessonID == lessonIOExam.LessonID &&
                            m.TypeOfExerciseID == typeItog.TypeOfExerciseID);
                        var markIOZ = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.StudentID == mark.StudentID &&
                            m.LessonID == lessonIOZ.LessonID &&
                            m.TypeOfExerciseID == typeItog.TypeOfExerciseID);

                        if (markExam == null || markZ == null || markIOExam == null || markIOZ == null)
                        {
                            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
                        }

                        var simpleMarks = await _context.Marks
                            .Where(m => m.FlagF == mark.FlagF &&
                                        m.StudentID == mark.StudentID &&
                                        m.SubjectID == SubjectID &&
                                        m.TypeOfExerciseID != markExam.TypeOfExerciseID &&
                                        m.TypeOfExerciseID != markIOExam.TypeOfExerciseID &&
                                        m.TypeOfExerciseID != markZ.TypeOfExerciseID &&
                                        m.TypeOfExerciseID != markIOZ.TypeOfExerciseID)
                            .ToListAsync();

                        List<double> simpleDoubleMarks = new();

                        foreach (var m in simpleMarks)
                        {
                            if (TryParseMarkValue(m.Value, out double number))
                            {
                                simpleDoubleMarks.Add(number);
                            }
                        }

                        if (mark.MarkID == markExam.MarkID)
                        {
                            if (new[] { "1", "2", "3" }.Contains(mark.Value))
                            {
                                markIOExam.Value = mark.Value;
                                markIOExam.ChangeCounter = 3;
                            }
                            else if (TryParseMarkValue(mark.Value, out double num))
                            {
                                double average = simpleDoubleMarks.Average();
                                double finalValue = average * 0.6 + num * 0.4;
                                markIOExam.Value = Math.Round(finalValue).ToString(CultureInfo.InvariantCulture);
                                markIOExam.ChangeCounter = 3;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        var markIA = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.SubjectID == SubjectID &&
                            m.FlagF == mark.FlagF &&
                            requiredTypeIds.Contains(m.TypeOfExerciseID) &&
                            m.StudentID == mark.StudentID);

                        var markIO = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.SubjectID == SubjectID &&
                            m.FlagF == mark.FlagF &&
                            m.TypeOfExerciseID == typeItog.TypeOfExerciseID &&
                            m.StudentID == mark.StudentID);

                        if (markIA == null || markIO == null)
                            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });

                        var marks = await _context.Marks
                            .Where(m => m.FlagF == mark.FlagF &&
                                        m.SubjectID == SubjectID &&
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
                        else
                        {
                            if (mark.TypeOfExerciseID != typeZachet.TypeOfExerciseID)
                            {
                                if (markIA.Value == "Недопуск")
                                {
                                    markIA.Value = "";
                                    markIO.Value = "";
                                }
                                else if (double.TryParse(markIA.Value, out double number))
                                {
                                    double average = doubleMarks.Average();
                                    double finalValue = average * 0.6 + number * 0.4;
                                    markIO.Value = Math.Round(finalValue).ToString(CultureInfo.InvariantCulture);
                                }
                            }
                            _context.Marks.Update(markIO);
                        }

                        await _context.SaveChangesAsync();
                    }
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

        public async Task<IActionResult> AdjustmentEdit(int? MarkID, int? GroupID, int? SubjectID)
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
        public async Task<IActionResult> AdjustmentEdit(int MarkID, int? GroupID, int? SubjectID, [Bind("MarkID,Value,Date,Comment,SignatureOfTeacher,HistoryOfMark,SubjectID,GroupID,LessonID,TypeOfExerciseID,DepartmentID,SpecialityID,ThemeID,StudentID,FlagX,FlagF,InstituteID,ChangeCounter")] Mark mark)
        {
            if (MarkID != mark.MarkID)
                return NotFound();

            if (!ModelState.IsValid)
                return View(mark);

            string teacher = _userNameService.GetDisplayName();

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
                        Teacher = teacher,
                        Log = $"Изменение отметки от {mark.Date:dd.MM.yyyy}, предмет: {subject.Name}, тема: {theme.Name}, тип занятия: {type.Name}, курсант/слушатель: {student.LastName} {student.Name[0]}.{student.Surname[0]}. группа: {group.Name}"
                    };
                    _context.Events.Add(e);
                    await _context.SaveChangesAsync();
                }
            }

            mark.SignatureOfTeacher = teacher;
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
                    var typeItog = types.FirstOrDefault(t => t.Name == "Итоговая отметка");
                    var typeKontrol = types.FirstOrDefault(t => t.Name == "Контрольное мероприятие");

                    if (typeZachet == null || typeDiffZachet == null || typeExam == null || typeItog == null || typeKontrol == null)
                        throw new InvalidOperationException("Не все типы занятий найдены в справочнике.");

                    var requiredTypeIds = new List<int>
                    {
                        typeZachet.TypeOfExerciseID,
                        typeDiffZachet.TypeOfExerciseID,
                        typeExam.TypeOfExerciseID
                    };

                    var IALessons = await _context.Lessons
                        .Where(l =>
                            l.GroupID == mark.GroupID &&
                            l.FlagF == mark.FlagF &&
                            l.SubjectID == mark.SubjectID &&
                            (l.TypeOfExerciseID == typeExam.TypeOfExerciseID || l.TypeOfExerciseID == typeZachet.TypeOfExerciseID))
                        .ToListAsync();

                    if (IALessons.Count > 1)
                    {
                        var student = await _context.Students.FindAsync(mark.StudentID);
                        var lessonExam = IALessons.FirstOrDefault(l => l.TypeOfExerciseID == typeExam.TypeOfExerciseID);
                        var lessonZ = IALessons.FirstOrDefault(l => l.TypeOfExerciseID == typeZachet.TypeOfExerciseID);
                        var lessonIOExam = await _context.Lessons.FirstOrDefaultAsync(l =>
                            l.FlagF == mark.FlagF &&
                            l.TypeOfExerciseID == typeItog.TypeOfExerciseID &&
                            l.SubjectID == SubjectID &&
                            l.GroupID == GroupID &&
                            l.Date.Year == lessonExam.Date.Year &&
                            l.Date.Month == lessonExam.Date.Month &&
                            l.Date.Day == lessonExam.Date.Day);
                        var lessonIOZ = await _context.Lessons.FirstOrDefaultAsync(l =>
                            l.FlagF == mark.FlagF &&
                            l.TypeOfExerciseID == typeItog.TypeOfExerciseID &&
                            l.SubjectID == SubjectID &&
                            l.GroupID == GroupID &&
                            l.Date.Year == lessonZ.Date.Year &&
                            l.Date.Month == lessonZ.Date.Month &&
                            l.Date.Day == lessonZ.Date.Day);

                        if (lessonExam == null || lessonZ == null || lessonIOExam == null || lessonIOZ == null)
                        {
                            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
                        }

                        var markExam = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.StudentID == mark.StudentID &&
                            m.LessonID == lessonExam.LessonID &&
                            m.TypeOfExerciseID == typeExam.TypeOfExerciseID);
                        var markZ = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.StudentID == mark.StudentID &&
                            m.LessonID == lessonZ.LessonID &&
                            m.TypeOfExerciseID == typeZachet.TypeOfExerciseID);
                        var markIOExam = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.StudentID == mark.StudentID &&
                            m.LessonID == lessonIOExam.LessonID &&
                            m.TypeOfExerciseID == typeItog.TypeOfExerciseID);
                        var markIOZ = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.StudentID == mark.StudentID &&
                            m.LessonID == lessonIOZ.LessonID &&
                            m.TypeOfExerciseID == typeItog.TypeOfExerciseID);

                        if (markExam == null || markZ == null || markIOExam == null || markIOZ == null)
                        {
                            return RedirectToAction("Journal", "Journals", new { GroupID, SubjectID });
                        }

                        var simpleMarks = await _context.Marks
                            .Where(m => m.FlagF == mark.FlagF &&
                                        m.StudentID == mark.StudentID &&
                                        m.SubjectID == SubjectID &&
                                        m.TypeOfExerciseID != markExam.TypeOfExerciseID &&
                                        m.TypeOfExerciseID != markIOExam.TypeOfExerciseID &&
                                        m.TypeOfExerciseID != markZ.TypeOfExerciseID &&
                                        m.TypeOfExerciseID != markIOZ.TypeOfExerciseID)
                            .ToListAsync();

                        var control_marks = simpleMarks.Where(m => m.TypeOfExerciseID == typeKontrol.TypeOfExerciseID);

                        List<double> simpleDoubleMarks = new();
                        List<double> control_double_marks = new();

                        foreach (var m in simpleMarks)
                        {
                            if (TryParseMarkValue(m.Value, out double number))
                            {
                                if (m.TypeOfExerciseID == typeKontrol.TypeOfExerciseID)
                                {
                                    control_double_marks.Add(number);
                                }
                                simpleDoubleMarks.Add(number);
                            }
                        }

                        if (mark.MarkID == markExam.MarkID)
                        {
                            if (new[] { "1", "2", "3" }.Contains(mark.Value))
                            {
                                markIOExam.Value = mark.Value;
                                markIOExam.ChangeCounter = 3;
                            }
                            else if (TryParseMarkValue(mark.Value, out double num))
                            {
                                double average = simpleDoubleMarks.Average();
                                double finalValue = average * 0.6 + num * 0.4;
                                markIOExam.Value = Math.Round(finalValue).ToString(CultureInfo.InvariantCulture);
                                markIOExam.ChangeCounter = 3;
                            }
                        }
                        else if (mark.TypeOfExerciseID != typeZachet.TypeOfExerciseID &&
                            mark.TypeOfExerciseID != typeDiffZachet.TypeOfExerciseID &&
                            mark.TypeOfExerciseID != typeExam.TypeOfExerciseID &&
                            mark.TypeOfExerciseID != typeItog.TypeOfExerciseID && mark.TypeOfExerciseID != typeKontrol.TypeOfExerciseID)
                        {
                            double average = simpleDoubleMarks.Average();
                            if (average < 4)
                            {
                                markExam.Value = "Недопуск";
                                markIOExam.Value = "Недопуск";
                                markZ.Value = "Недопуск";
                                markIOZ.Value = "Недопуск";
                            }
                            else if (average >= 4)
                            {
                                List<double> targets = new() { 1, 2, 3 };
                                if (control_marks.Count() != control_double_marks.Count() || targets.Any(m => control_double_marks.Contains(m)))
                                {
                                    markExam.Value = "Недопуск";
                                    markIOExam.Value = "Недопуск";
                                    markZ.Value = "Недопуск";
                                    markIOZ.Value = "Недопуск";
                                }
                                else if (markZ.Value == "Недопуск")
                                {
                                    markZ.Value = "";
                                    markIOZ.Value = "";
                                }
                                else
                                {
                                    if (markZ.Value == "З")
                                    {
                                        if (TryParseMarkValue(markExam.Value, out double exam))
                                        {
                                            if (exam != 1 || exam != 2 || exam != 3)
                                            {
                                                markIOExam.Value = Math.Round(average * 0.6 + exam * 0.4).ToString(CultureInfo.InvariantCulture);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (mark.TypeOfExerciseID == typeKontrol.TypeOfExerciseID)
                        {
                            if (TryParseMarkValue(mark.Value, out double controlMark))
                            {
                                if (controlMark == 1 || controlMark == 2 || controlMark == 3)
                                {
                                    markExam.Value = "Недопуск";
                                    markIOExam.Value = "Недопуск";
                                    markZ.Value = "Недопуск";
                                    markIOZ.Value = "Недопуск";
                                }
                                else
                                {
                                    double average = simpleDoubleMarks.Average();
                                    if (markZ.Value == "З")
                                    {
                                        if (TryParseMarkValue(markExam.Value, out double exam))
                                        {
                                            if (exam != 1 || exam != 2 || exam != 3)
                                            {
                                                markIOExam.Value = Math.Round(average * 0.6 + exam * 0.4).ToString(CultureInfo.InvariantCulture);
                                            }
                                        }
                                    }
                                    else if (markZ.Value == "Недопуск")
                                    {
                                        List<double> targets = new() { 1, 2, 3 };
                                        if (average >= 4 && control_marks.Count() == control_double_marks.Count() && !targets.Any(m => control_double_marks.Contains(m)))
                                        {
                                            markZ.Value = "";
                                            markIOZ.Value = "";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                markExam.Value = "Недопуск";
                                markIOExam.Value = "Недопуск";
                                markZ.Value = "Недопуск";
                                markIOZ.Value = "Недопуск";
                            }
                        }
                        else if (mark.TypeOfExerciseID == typeZachet.TypeOfExerciseID)
                        {
                            List<double> targets = new() { 1, 2, 3 };
                            double average = simpleDoubleMarks.Average();
                            if (average >= 4 && control_marks.Count() == control_double_marks.Count() && !targets.Any(m => control_double_marks.Contains(m)))
                            {
                                if (mark.Value == "З")
                                {
                                    markExam.Value = "";
                                    markIOExam.Value = "";
                                    markIOZ.Value = "Зачтено";
                                }
                                else
                                {
                                    markExam.Value = "Недопуск";
                                    markIOExam.Value = "Недопуск";
                                    markIOZ.Value = "Не зачтено";
                                }
                            }
                            else
                            {
                                markExam.Value = "Недопуск";
                                markIOExam.Value = "Недопуск";
                                markZ.Value = "Недопуск";
                                markIOZ.Value = "Недопуск";
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        var markIA = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.FlagF == mark.FlagF &&
                            requiredTypeIds.Contains(m.TypeOfExerciseID) &&
                            m.StudentID == mark.StudentID);

                        var markIO = await _context.Marks.FirstOrDefaultAsync(m =>
                            m.FlagF == mark.FlagF &&
                            m.TypeOfExerciseID == typeItog.TypeOfExerciseID &&
                            m.StudentID == mark.StudentID);

                        if (markIA == null || markIO == null)
                            return RedirectToAction("AdjustmentJournal", "Journals", new { GroupID, SubjectID });

                        var marks = await _context.Marks
                            .Where(m => m.FlagF == mark.FlagF &&
                                        m.StudentID == mark.StudentID &&
                                        m.TypeOfExerciseID != markIA.TypeOfExerciseID &&
                                        m.TypeOfExerciseID != markIO.TypeOfExerciseID)
                            .ToListAsync();

                        List<double> doubleMarks = new();
                        List<double> doubleControlMarks = new();
                        List<Mark> controlMarks = new();

                        foreach (var m in marks)
                        {
                            if (TryParseMarkValue(m.Value, out double number))
                            {
                                doubleMarks.Add(number);
                                if (m.TypeOfExerciseID == typeKontrol.TypeOfExerciseID)
                                    doubleControlMarks.Add(number);
                            }

                            if (m.TypeOfExerciseID == typeKontrol.TypeOfExerciseID)
                            {
                                controlMarks.Add(m);
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

                        if (lowAverage || hasLowControlMark || noControlMarks || controlMarks.Count != doubleControlMarks.Count)
                        {
                            markIA.Value = "Недопуск";
                            markIO.Value = "Недопуск";
                            markIA.ChangeCounter = 3;
                            markIO.ChangeCounter = 3;
                            _context.Marks.Update(markIA);
                            _context.Marks.Update(markIO);
                        }
                        else
                        {
                            if (mark.TypeOfExerciseID != typeZachet.TypeOfExerciseID && mark.TypeOfExerciseID != typeItog.TypeOfExerciseID && !requiredTypeIds.Contains(mark.TypeOfExerciseID))
                            {
                                if (markIA.Value == "Недопуск")
                                {
                                    markIA.Value = "";
                                    markIO.Value = "";
                                    markIA.ChangeCounter = 0;
                                }
                                else if (double.TryParse(markIA.Value, out double number))
                                {
                                    double average = doubleMarks.Average();
                                    double finalValue = average * 0.6 + number * 0.4;
                                    markIO.Value = Math.Round(finalValue).ToString(CultureInfo.InvariantCulture);
                                }
                            }
                            else if (mark.TypeOfExerciseID == typeItog.TypeOfExerciseID)
                            {
                                markIO.Value = mark.Value;
                            }
                            _context.Marks.Update(markIO);
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MarkExists(mark.MarkID))
                    return NotFound();
                throw;
            }



            return RedirectToAction("AdjustedJournal", "Journals", new { GroupID, SubjectID });
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
