using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Portal.Data;
using Portal.Models;
using Portal.Services;
using Portal.ViewModel;
using Portal.ViewModel.Statement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class StudentsController : Controller
    {
        private readonly AcademyContext _context;
        private readonly StudentAverageMarkService _studentAverageMarkService;

        public StudentsController(AcademyContext context, StudentAverageMarkService studentAverageMarkService)
        {
            _context = context;
            _studentAverageMarkService = studentAverageMarkService;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var academyContext = _context.Students
                .OrderByDescending(s => s.Status)
                    .ThenBy(s => s.LastName)
                .Include(s => s.Group);
            return View(await academyContext.ToListAsync());
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH, User")]
        public async Task<IActionResult> Details(int? id, string searchString)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .AsSplitQuery()
                .Include(s => s.Group.Speciality.Institute)
                .Include(s => s.Group.Journals)
                    .ThenInclude(j => j.Subject)
                .Include(s => s.Marks)
                    .ThenInclude(m => m.Theme)
                        .ThenInclude(t => t.Subject)
                .FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
            {
                return NotFound();
            }

            // Получаем типы занятий
            var typeSZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Семинарское занятие");
            var typePZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Практическое занятие");
            var typeLZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лабораторное занятие");
            var typeL = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лекция");
            var typeKM = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Контрольное мероприятие");
            var typeGPZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Городское практическое занятие");

            var typeEKZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Экзамен");
            var typeDZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дифференцированный зачёт");
            var typeZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Зачёт");
            var typeF = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая отметка");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");

            // Типы для учебных отметок
            var studyTypeIds = new HashSet<int>
    {
        typeKM?.TypeOfExerciseID ?? 0,
        typeGPZ?.TypeOfExerciseID ?? 0,
        typeSZ?.TypeOfExerciseID ?? 0,
        typePZ?.TypeOfExerciseID ?? 0,
        typeLZ?.TypeOfExerciseID ?? 0,
        typeL?.TypeOfExerciseID ?? 0
    };

            List<Mark> marks = new();
            var date = DateTime.Now.AddYears(-1);

            // Получаем ВСЕ отметки студента
            var allMarks = student.Marks.ToList();

            // ========================================
            // ОПРЕДЕЛЯЕМ УЧЕБНЫЙ ГОД
            // ========================================
            int startYear, endYear;
            if (DateTime.Now.Month >= 9)
            {
                startYear = DateTime.Now.Year;
                endYear = DateTime.Now.Year + 1;
            }
            else
            {
                startYear = DateTime.Now.Year - 1;
                endYear = DateTime.Now.Year;
            }

            // ========================================
            // ФИЛЬТРУЕМ ОТМЕТКИ В ЗАВИСИМОСТИ ОТ СЕМЕСТРА
            // ========================================

            if (DateTime.Now.Month >= 9 && DateTime.Now.Month <= 12)
            {
                var semesterMarks = allMarks
                    .Where(m =>
                        studyTypeIds.Contains(m.TypeOfExerciseID) &&
                        m.Date.Year == DateTime.Now.Year &&
                        m.Date.Month >= 9 && m.Date.Month <= 12
                    )
                    .ToList();

                var incompleteMarks = allMarks.Where(m => m.FlagF == 0).ToList();

                var allIds = new HashSet<int>();
                marks = semesterMarks
                    .Concat(incompleteMarks)
                    .Where(m => allIds.Add(m.MarkID))
                    .ToList();
            }
            else if (DateTime.Now.Month == 1)
            {
                var semesterMarks = allMarks
                    .Where(m =>
                        studyTypeIds.Contains(m.TypeOfExerciseID) &&
                        ((m.Date.Year == DateTime.Now.Year && m.Date.Month == 1) ||
                         (m.Date.Year == date.Year && m.Date.Month >= 9 && m.Date.Month <= 12))
                    )
                    .ToList();

                var incompleteMarks = allMarks.Where(m => m.FlagF == 0).ToList();

                var allIds = new HashSet<int>();
                marks = semesterMarks
                    .Concat(incompleteMarks)
                    .Where(m => allIds.Add(m.MarkID))
                    .ToList();
            }
            else
            {
                var semesterMarks = allMarks
                    .Where(m =>
                        studyTypeIds.Contains(m.TypeOfExerciseID) &&
                        m.Date.Year == DateTime.Now.Year &&
                        m.Date.Month >= 2 && m.Date.Month <= 8
                    )
                    .ToList();

                var incompleteMarks = allMarks.Where(m => m.FlagF == 0).ToList();

                var allIds = new HashSet<int>();
                marks = semesterMarks
                    .Concat(incompleteMarks)
                    .Where(m => allIds.Add(m.MarkID))
                    .ToList();
            }

            // ========================================
            // ИЗВЛЕКАЕМ ЧИСЛОВЫЕ ОТМЕТКИ ДЛЯ РАСЧЕТОВ
            // ========================================
            var numericMarks = marks
                .Select(m => double.TryParse(m.Value, out var value) ? value : (double?)null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            // ========================================
            // ТЕКУЩИЙ СРЕДНИЙ БАЛЛ СТУДЕНТА 
            // (СРЕДНЕЕ АРИФМЕТИЧЕСКОЕ СРЕДНИХ БАЛЛОВ ПО ПРЕДМЕТАМ)
            // ========================================
            double raiting = 0;

            // Группируем отметки по предметам и считаем средний балл по каждому предмету
            var subjectAverages = marks
                .Where(m => studyTypeIds.Contains(m.TypeOfExerciseID))
                .GroupBy(m => m.SubjectID)
                .Select(g => new
                {
                    SubjectID = g.Key,
                    Average = g
                        .Select(m => double.TryParse(m.Value, out var value) ? value : (double?)null)
                        .Where(v => v.HasValue)
                        .Select(v => v.Value)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .Where(s => s.Average > 0)
                .ToList();

            // Среднее арифметическое средних баллов по предметам
            if (subjectAverages.Any())
            {
                raiting = Math.Round(subjectAverages.Average(s => s.Average), 3, MidpointRounding.AwayFromZero);
            }

            // ========================================
            // УЧЕБНЫЙ ГОД
            // ========================================
            string yearsStudy = $"{startYear}/{endYear}";

            // ========================================
            // ОЦЕНОЧНЫЕ ПОКАЗАТЕЛИ СЛУШАТЕЛЯ/КУРСАНТА
            // ========================================
            Dictionary<int, int> marksNumber = new();
            Dictionary<int, decimal> marksPercent = new();

            for (int i = 1; i <= 10; i++)
            {
                int count = numericMarks.Count(x => (int)x == i);
                marksNumber.Add(i, count);

                marksPercent.Add(i, numericMarks.Any()
                    ? Math.Round((decimal)count / numericMarks.Count * 100, 3, MidpointRounding.AwayFromZero)
                    : 0);
            }

            // ========================================
            // ОЦЕНКА ПОСЕЩАЕМОСТИ
            // ========================================
            int b = 0, nr = 0, o = 0, km = 0, r = 0, nb = 0, num = 0;
            double mNumber = student.Marks.Count;

            foreach (var mark in student.Marks)
            {
                if (mark.Value == "Б") b++;
                else if (mark.Value == "НР") nr++;
                else if (mark.Value == "О") o++;
                else if (mark.Value == "КМ") km++;
                else if (mark.Value == "Р") r++;
                else if (mark.Value == "НБ") nb++;
                else num++;
            }

            Dictionary<string, int> attendanceNumber = new()
    {
        {"Болезнь", b},
        {"Наряд", nr},
        {"Отпуск", o},
        {"Коммандировка", km},
        {"Отсутствие по мотивированный рапорт", r},
        {"Отсутствие без уважительной причины", nb},
        {"Присутствие", num}
    };

            Dictionary<string, double> attendancePercent = new()
    {
        {"Болезнь", mNumber > 0 ? Math.Round((double)b / mNumber * 100, 3, MidpointRounding.AwayFromZero) : 0},
        {"Наряд", mNumber > 0 ? Math.Round((double)nr / mNumber * 100, 3, MidpointRounding.AwayFromZero) : 0},
        {"Отпуск", mNumber > 0 ? Math.Round((double)o / mNumber * 100, 3, MidpointRounding.AwayFromZero) : 0},
        {"Коммандировка", mNumber > 0 ? Math.Round((double)km / mNumber * 100, 3, MidpointRounding.AwayFromZero) : 0},
        {"Отсутствие по мотивированный рапорт", mNumber > 0 ? Math.Round((double)r / mNumber * 100, 3, MidpointRounding.AwayFromZero) : 0},
        {"Отсутствие без уважительной причины", mNumber > 0 ? Math.Round((double)nb / mNumber * 100, 3, MidpointRounding.AwayFromZero) : 0},
        {"Присутствие", mNumber > 0 ? Math.Round((double)num / mNumber * 100, 3, MidpointRounding.AwayFromZero) : 0}
    };

            // ========================================
            // ТЕКУЩИЙ ОБЩИЙ СРЕДНИЙ БАЛЛ И СРЕДНИЙ БАЛ ЗА ПРЕДМЕТ ПО МЕСЯЦАМ
            // ========================================
            int subjectID = 0;

            if (!string.IsNullOrEmpty(searchString))
            {
                if (int.TryParse(searchString, out var subID))
                {
                    subjectID = subID;
                }
            }
            else if (student.Group.Journals.Count != 0)
            {
                subjectID = student.Group.Journals.FirstOrDefault().SubjectID;
            }

            var subject = await _context.Subjects.FindAsync(subjectID);
            var subjectMarks = marks.Where(m => m.SubjectID == subjectID).ToList();

            Dictionary<string, string> raitingTimeSubject = new();
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                        "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };

            foreach (string month in months)
            {
                int monthNumber = Array.IndexOf(months, month) + 1;

                // Успеваемость по предмету по месяцам
                var monthMarks = subjectMarks
                    .Where(m => m.Date.Month == monthNumber)
                    .ToList();

                var monthNumericMarks = monthMarks
                    .Select(m => double.TryParse(m.Value, out var value) ? value : (double?)null)
                    .Where(v => v.HasValue)
                    .Select(v => v.Value)
                    .ToList();

                double subjectAvg = monthNumericMarks.Any()
                    ? Math.Round(monthNumericMarks.Average(), 3, MidpointRounding.AwayFromZero)
                    : 0;
                raitingTimeSubject.Add(month, subjectAvg.ToString("0.000").Replace(",", "."));
            }

            // ========================================
            // ОБЩАЯ СРЕДНЕМЕСЯЧНАЯ УСПЕВАЕМОСТЬ (ЗА ВЕСЬ УЧЕБНЫЙ ГОД)
            // ========================================
            Dictionary<string, string> raitingTime = new();

            // Получаем отметки за учебный год
            var yearMarks = allMarks
                .Where(m => studyTypeIds.Contains(m.TypeOfExerciseID))
                .ToList();

            // Массив месяцев для представления (сентябрь-август)
            string[] monthNames = { "Сентябрь", "Октябрь", "Ноябрь", "Декабрь",
                            "Январь", "Февраль", "Март", "Апрель",
                            "Май", "Июнь", "Июль", "Август" };

            // Сентябрь-Декабрь (startYear)
            for (int month = 9; month <= 12; month++)
            {
                string monthName = GetMonthName(month);
                var monthMarks = yearMarks
                    .Where(m => m.Date.Month == month && m.Date.Year == startYear)
                    .ToList();

                double avg = CalculateAverageMark(monthMarks);
                raitingTime.Add(monthName, avg.ToString("0.000").Replace(",", "."));
            }

            // Январь-Август (endYear)
            for (int month = 1; month <= 8; month++)
            {
                string monthName = GetMonthName(month);
                var monthMarks = yearMarks
                    .Where(m => m.Date.Month == month && m.Date.Year == endYear)
                    .ToList();

                double avg = CalculateAverageMark(monthMarks);
                raitingTime.Add(monthName, avg.ToString("0.000").Replace(",", "."));
            }

            // ========================================
            // ДИАГРАММА ПРЕДМЕТОВ (РОЗА ВЕТРОВ)
            // ========================================
            var journals = student.Group.Journals.ToList();
            List<object> radar = new();

            foreach (var journal in journals)
            {
                var studentSubjectMarks = marks
                    .Where(m =>
                        m.SubjectID == journal.SubjectID &&
                        studyTypeIds.Contains(m.TypeOfExerciseID)
                    );

                var subjectNumericMarks = studentSubjectMarks
                    .Select(m => double.TryParse(m.Value, out var value) ? value : (double?)null)
                    .Where(v => v.HasValue)
                    .Select(v => v.Value)
                    .ToList();

                if (subjectNumericMarks.Any())
                {
                    double valRaiting = Math.Round(subjectNumericMarks.Average(), 3, MidpointRounding.AwayFromZero);
                    radar.Add(new
                    {
                        Subject = journal.Subject.ShortName?.ToString() ?? journal.Subject.Name,
                        Value = valRaiting.ToString("0.000").Replace(",", ".")
                    });
                }
            }

            // ========================================
            // ОТРИЦАТЕЛЬНЫЕ РЕЗУЛЬТАТЫ
            // ========================================
            var negativeMarks = marks
                .Where(m => m.Value == "1" || m.Value == "2" || m.Value == "3")
                .ToList();

            // ========================================
            // ИТОГОВЫЕ РЕЗУЛЬТАТЫ ОБУЧЕНИЯ
            // ========================================
            var statementMarks = await _context.StatementMarks
                .Where(m => m.StudentID == id)
                .ToListAsync();

            List<FinalMark> finalMarks = new();
            foreach (var mark in statementMarks)
            {
                TypeOfExercise t = await _context.Types.FindAsync(mark.TypeOfExerciseID);
                finalMarks.Add(new FinalMark { Mark = mark, Type = t });
            }

            // ========================================
            // РЕЗУЛЬТАТЫ ОБУЧЕНИЯ ПО ПРЕДМЕТАМ
            // ========================================
            List<MarkSubjectFinal> markSubjectFinals = new();

            foreach (var journal in journals)
            {
                var mrks = marks
                    .Where(m =>
                        m.SubjectID == journal.SubjectID &&
                        studyTypeIds.Contains(m.TypeOfExerciseID)
                    );

                var simplemrks = mrks
                    .Select(m => double.TryParse(m.Value, out var vm) ? vm : (double?)null)
                    .Where(v => v.HasValue)
                    .Select(v => v.Value)
                    .ToList();

                var controlMarks = allMarks
                    .Where(m =>
                        m.SubjectID == journal.SubjectID &&
                        (m.TypeOfExerciseID == typeEKZ?.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeDZ?.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeZ?.TypeOfExerciseID)
                    )
                    .ToList();

                var fMarks = allMarks
                    .Where(m =>
                        m.SubjectID == journal.SubjectID &&
                        m.TypeOfExerciseID == typeF?.TypeOfExerciseID
                    )
                    .ToList();

                var kMarks = allMarks
                    .Where(m =>
                        m.SubjectID == journal.SubjectID &&
                        (m.TypeOfExerciseID == typeKP?.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeKR?.TypeOfExerciseID)
                    )
                    .ToList();

                MarkSubjectFinal msf = new()
                {
                    Subject = journal.Subject,
                    Value = simplemrks.Any() ? Math.Round(simplemrks.Average(), 3, MidpointRounding.AwayFromZero) : 0,
                    ControlMarks = controlMarks,
                    FinalMarks = fMarks,
                    ValueK = kMarks
                };
                markSubjectFinals.Add(msf);
            }

            // ========================================
            // ФОРМИРОВАНИЕ VIEWMODEL
            // ========================================
            StudentDetailsView studentDetailsView = new()
            {
                Student = student,
                AttendancePercent = attendancePercent,
                AttendanceNumber = attendanceNumber,
                MarkSubjectFinals = markSubjectFinals,
                FinalMarks = finalMarks.OrderBy(m => m.Mark.Date).ToList(),
                NegativeMarks = negativeMarks,
                Radar = radar,
                Raiting = raiting,
                MarksNumber = marksNumber,
                MarksPercent = marksPercent,
                YearsStudy = yearsStudy,
                RaitingTimeSubject = raitingTimeSubject,
                Subject = subject,
                RaitingTime = raitingTime
            };

            return View(studentDetailsView);
        }

        // Вспомогательные методы
        private string GetMonthName(int month)
        {
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                        "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            return months[month - 1];
        }

        private double CalculateAverageMark(List<Mark> marks)
        {
            var numericMarks = marks
                .Select(m => double.TryParse(m.Value, out var value) ? value : (double?)null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            return numericMarks.Any()
                ? Math.Round(numericMarks.Average(), 3, MidpointRounding.AwayFromZero)
                : 0;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create(int? id)
        {
            if (id != null)
            {
                var groupsQuery = from g in _context.Groups
                                  where g.GroupID == id
                                  select g;
                ViewBag.GroupID = new SelectList(groupsQuery, "GroupID", "Name");
            }
            else
            {
                var groupsQuery = from g in _context.Groups
                                  orderby g.Name
                                  select g;
                ViewBag.GroupID = new SelectList(groupsQuery, "GroupID", "Name");
            }
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentID,GroupID,Name,Surname,LastName,PlaceOfBirth,DateOfBirth,InstituteID")] Student student)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var groups = _context.Groups.Include(g => g.Students);
                    var group = await groups.FirstOrDefaultAsync(g => g.GroupID == student.GroupID);
                    student.InstituteID = group.InstituteID;
                    student.Status = true;
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                    var marks = _context.Marks.Where(m => m.StudentID == group.Students.FirstOrDefault().StudentID);
                    if (marks.Any())
                    {
                        foreach (var mark in marks)
                        {
                            Mark newMark = new();
                            newMark.Value = "-";
                            newMark.Date = mark.Date;
                            newMark.FlagF = mark.FlagF;
                            newMark.InstituteID = mark.InstituteID;
                            newMark.SubjectID = mark.SubjectID;
                            newMark.GroupID = mark.GroupID;
                            newMark.LessonID = mark.LessonID;
                            newMark.TypeOfExerciseID = mark.TypeOfExerciseID;
                            newMark.DepartmentID = mark.DepartmentID;
                            newMark.SpecialityID = mark.SpecialityID;
                            newMark.ThemeID = mark.ThemeID;
                            newMark.StudentID = student.StudentID;
                            _context.Marks.Add(newMark);
                        }
                    }
                    await _context.SaveChangesAsync();
                    var statementMarks = _context.StatementMarks.Where(m => m.StudentID == group.Students.FirstOrDefault().StudentID);
                    if (statementMarks.Any())
                    {
                        foreach (var mark in statementMarks)
                        {
                            StatementMark newStatementMark = new();
                            newStatementMark.Value = "";
                            newStatementMark.Date = mark.Date;
                            newStatementMark.InstituteID = mark.InstituteID;
                            newStatementMark.SpecialityID = mark.SpecialityID;
                            newStatementMark.GroupID = mark.SpecialityID;
                            newStatementMark.TypeOfExerciseID = mark.TypeOfExerciseID;
                            newStatementMark.StatementLessonID = mark.StatementLessonID;
                            newStatementMark.StudentID = student.StudentID;
                            _context.StatementMarks.Add(newStatementMark);
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
            }
            catch (RetryLimitExceededException)
            {
                ModelState.AddModelError("", "Возникла ошибка обратитесь к администратору.");
            }

            var groupsQuery = from g in _context.Groups
                              orderby g.Name
                              select g;
            ViewBag.GroupID = new SelectList(groupsQuery, "GroupID", "Name", student.GroupID);
            return View(student);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            ViewData["GroupID"] = new SelectList(_context.Groups, "GroupID", "Name", student.GroupID);
            return View(student);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentID,GroupID,Name,Surname,LastName,PlaceOfBirth,DateOfBirth,Status,InstituteID")] Student student)
        {
            if (id != student.StudentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentID))
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
            ViewData["GroupID"] = new SelectList(_context.Groups, "GroupID", "Name", student.GroupID);
            return View(student);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> SetStatus(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(d => d.StudentID == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, [Bind("StudentID,GroupID,Name,Surname,LastName,PlaceOfBirth,DateOfBirth,Status,InstituteID")] Student student)
        {
            if (id != student.StudentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (student.Status == true)
                    {
                        student.Status = false;
                    }
                    else
                    {
                        student.Status = true;
                    }
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (!StudentExists(student.StudentID))
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
            return View(student);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Group)
                .FirstOrDefaultAsync(m => m.StudentID == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentID == id);
        }
    }
}
