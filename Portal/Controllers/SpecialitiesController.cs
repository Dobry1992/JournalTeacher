using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Services;
using Portal.ViewModel;
using Portal.ViewModel.Raiting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class SpecialitiesController : Controller
    {
        private readonly AcademyContext _context;
        private readonly StudentAverageMarkService _studentAverageMarkService;
        private readonly InstituteAverageMarkService _instituteAverage;

        public SpecialitiesController(AcademyContext context, StudentAverageMarkService studentAverageMarkService, InstituteAverageMarkService instituteAverage)
        {
            _context = context;
            _studentAverageMarkService = studentAverageMarkService;
            _instituteAverage = instituteAverage;
        }

        public IActionResult NavMenuJournal()
        {
            return PartialView("_NavMenuJournal");
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var specialities = _context.Specialities
                .OrderBy(s => s.Arch)
                    .ThenBy(s => s.Name)
                .Include(g => g.Groups);
            return View(await specialities.ToListAsync());
        }

        public async Task<IActionResult> ChooseGroup()
        {
            var specialities = _context.Specialities
                  .OrderBy(s => s.Arch)
                     .ThenBy(s => s.Name)
                 .Include(g => g.Groups);
            return View(await specialities.ToListAsync());
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var speciality = await _context.Specialities
                .FirstOrDefaultAsync(m => m.SpecialityID == id);
            if (speciality == null)
            {
                return NotFound();
            }

            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create()
        {
            ViewData["InstituteID"] = new SelectList(_context.Institutes, "InstituteID", "Name");
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SpecialityID,Name,TimeOFStudy,InstituteID")] Speciality speciality)
        {
            if (ModelState.IsValid)
            {
                _context.Add(speciality);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var speciality = await _context.Specialities.FindAsync(id);
            if (speciality == null)
            {
                return NotFound();
            }
            ViewData["InstituteID"] = new SelectList(_context.Institutes, "InstituteID", "Name");
            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("SpecialityID,Name,TimeOFStudy,Arch,InstituteID")] Speciality speciality)
        {
            if (id != speciality.SpecialityID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(speciality);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpecialityExists(speciality.SpecialityID))
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
            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var speciality = await _context.Specialities
                .FirstOrDefaultAsync(m => m.SpecialityID == id);
            if (speciality == null)
            {
                return NotFound();
            }

            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var speciality = await _context.Specialities.FindAsync(id);
            _context.Specialities.Remove(speciality);
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

            var speciality = await _context.Specialities
                .FirstOrDefaultAsync(d => d.SpecialityID == id);

            if (speciality == null)
            {
                return NotFound();
            }

            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, [Bind("SpecialityID,Name,TimeOFStudy,Arch,InstituteID")] Speciality speciality)
        {
            if (id != speciality.SpecialityID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (speciality.Arch == true)
                    {
                        speciality.Arch = false;
                    }
                    else
                    {
                        speciality.Arch = true;
                    }
                    _context.Update(speciality);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (!SpecialityExists(speciality.SpecialityID))
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
            return View(speciality);
        }

        [Authorize]
        public async Task<IActionResult> Analytics(int id, int year)
        {
            if (id == 0)
            {
                return NotFound();
            }

            // Получаем специальность
            var speciality = await _context.Specialities.FindAsync(id);
            if (speciality == null)
            {
                return NotFound();
            }

            // Получаем группы
            var groups = await _context.Groups
                .Where(g => g.SpecialityID == id && g.DateEnter.Year == year)
                .ToListAsync();

            var groupIds = groups.Select(g => g.GroupID).ToList();

            if (!groupIds.Any())
            {
                return View(new SpecialityAnalyticViewModel
                {
                    SpecialityID = id,
                    SpecialityName = speciality.Name,
                    EnterYear = year,
                    StudentNumber = 0,
                    GroupNumber = 0,
                    TimeRaiting = new Dictionary<string, string>()
                });
            }

            // Получаем типы занятий
            var typeSZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Семинарское занятие");
            var typePZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Практическое занятие");
            var typeLZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лабораторное занятие");
            var typeL = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лекция");
            var typeKM = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Контрольное мероприятие");
            var typeGPZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Городское практическое занятие");

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
            List<double> marksAverage = new();
            string term;
            var date = DateTime.Now.AddYears(-1);

            // Получаем ВСЕ отметки для групп специальности
            var allMarks = await _context.Marks
                .Where(m => m.SpecialityID == id && groupIds.Contains(m.GroupID))
                .ToListAsync();

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
            // ФИЛЬТРУЕМ ОТМЕТКИ ДЛЯ РЕЙТИНГОВ (ТОЛЬКО ТЕКУЩИЙ СЕМЕСТР)
            // ========================================

            if (DateTime.Now.Month >= 9 && DateTime.Now.Month <= 12)
            {
                term = "первый семестр";

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
                term = "первый семестр";

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
                term = "второй семестр";

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

            // Извлекаем числовые отметки для расчетов
            foreach (var mark in marks)
            {
                if (double.TryParse(mark.Value, out var m))
                {
                    marksAverage.Add(m);
                }
            }

            // ========================================
            // КОЛИЧЕСТВО ОБУЧАЮЩИХСЯ
            // ========================================
            var students = await _context.Students
                .Include(s => s.Group)
                .Where(s => groupIds.Contains(s.GroupID) && s.Status == true)
                .AsNoTracking()
                .ToListAsync();
            int studentNumber = students.Count;

            // ========================================
            // РЕЙТИНГ УЧАЩИХСЯ
            // ========================================
            List<StudentRaiting> studentRaitings = new();
            foreach (var student in students)
            {
                double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, marks);
                studentRaitings.Add(new StudentRaiting
                {
                    Group = student.Group,
                    Student = student,
                    Raiting = studentRating ?? 0
                });
            }

            // ========================================
            // РЕЙТИНГ УЧЕБНЫХ ГРУПП
            // ========================================
            List<InstGroupRaiting> groupsRating = new();
            foreach (var group in groups)
            {
                var groupStudents = students
                    .Where(s => s.GroupID == group.GroupID)
                    .ToList();

                var groupMarks = marks
                    .Where(m => m.GroupID == group.GroupID)
                    .ToList();

                List<double> groupStudentRatings = new();
                foreach (var student in groupStudents)
                {
                    double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, groupMarks);
                    if (studentRating.HasValue && studentRating.Value > 0)
                    {
                        groupStudentRatings.Add(studentRating.Value);
                    }
                }

                double groupRating = groupStudentRatings.Any()
                    ? Math.Round(groupStudentRatings.Average(), 3, MidpointRounding.AwayFromZero)
                    : 0;

                groupsRating.Add(new InstGroupRaiting
                {
                    Group = group,
                    Raiting = groupRating
                });
            }

            // ========================================
            // СРЕДНИЙ БАЛЛ КУРСА
            // ========================================
            double courseRating = 0;
            var filterGroupsRating = groupsRating
                .Where(m => m.Raiting > 0)
                .ToList();

            if (filterGroupsRating.Any())
            {
                courseRating = Math.Round(filterGroupsRating.Average(g => g.Raiting), 3, MidpointRounding.AwayFromZero);
            }

            // ========================================
            // ОЦЕНОЧНЫЕ ПОКАЗАТЕЛИ КУРСА
            // ========================================
            Dictionary<int, int> marksNumber = new();
            Dictionary<int, decimal> marksPercent = new();

            for (int i = 1; i <= 10; i++)
            {
                int count = marksAverage.Where(x => (int)x == i).Count();
                marksNumber.Add(i, count);

                marksPercent.Add(i, marksAverage.Any()
                    ? Math.Round((decimal)count / marksAverage.Count * 100, 3, MidpointRounding.AwayFromZero)
                    : 0);
            }

            // ========================================
            // СРЕДНЯЯ МЕСЯЧНАЯ УСПЕВАЕМОСТЬ
            // ========================================
            Dictionary<string, string> raitingTime = new();

            // Массив месяцев
            string[] monthNames = { "Сентябрь", "Октябрь", "Ноябрь", "Декабрь",
                            "Январь", "Февраль", "Март", "Апрель",
                            "Май", "Июнь", "Июль", "Август" };

            // Получаем отметки за учебный год
            var yearMarks = allMarks
                .Where(m => studyTypeIds.Contains(m.TypeOfExerciseID))
                .ToList();

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
            // УЧЕБНЫЙ ГОД
            // ========================================
            string yearsStudy = $"{startYear}/{endYear}";

            // ========================================
            // ЛУЧШИЙ И ХУДШИЙ СТУДЕНТ (для ViewBag)
            // ========================================
            var bestStudent = studentRaitings
                .Where(s => s.Raiting > 0)
                .OrderByDescending(s => s.Raiting)
                .FirstOrDefault();

            var worstStudent = studentRaitings
                .Where(s => s.Raiting > 0)
                .OrderBy(s => s.Raiting)
                .FirstOrDefault();

            // ========================================
            // ЛУЧШАЯ И ХУДШАЯ ГРУППА (для ViewBag)
            // ========================================
            var bestGroup = groupsRating
                .Where(g => g.Raiting > 0)
                .OrderByDescending(g => g.Raiting)
                .FirstOrDefault();

            var worstGroup = groupsRating
                .Where(g => g.Raiting > 0)
                .OrderBy(g => g.Raiting)
                .FirstOrDefault();

            // ========================================
            // ViewBag
            // ========================================
            ViewBag.BestStudent = bestStudent ?? new StudentRaiting { Raiting = 0 };
            ViewBag.WorseStudent = worstStudent ?? new StudentRaiting { Raiting = 0 };
            ViewBag.BestGroup = bestGroup ?? new InstGroupRaiting { Raiting = 0 };
            ViewBag.WorseGroup = worstGroup ?? new InstGroupRaiting { Raiting = 0 };

            // ========================================
            // ФОРМИРОВАНИЕ VIEWMODEL
            // ========================================
            SpecialityAnalyticViewModel specialityAnalyticViewModel = new()
            {
                SpecialityID = id,
                SpecialityName = speciality.Name,
                Term = term,
                Raiting = Math.Round(courseRating, 3, MidpointRounding.AwayFromZero),
                StudentNumber = studentNumber,
                GroupNumber = groups.Count,
                EnterYear = year,
                StudentsRaiting = studentRaitings.OrderByDescending(s => s.Raiting).ToList(),
                GroupsRaiting = groupsRating.OrderByDescending(g => g.Raiting).ToList(),
                MarksNumber = marksNumber,
                MarksPercent = marksPercent,
                TimeRaiting = raitingTime,
                Year = yearsStudy
            };

            return View(specialityAnalyticViewModel);
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

        [Authorize]
        public async Task<IActionResult> CourseStatement(int id, int year)
        {
            var speciality = await _context.Specialities.FindAsync(id);

            var groups = await _context.Groups
                .Where(g => g.SpecialityID == id && g.DateEnter.Year == year)
                .ToListAsync();

            if (!groups.Any())
                return NotFound("Групп для специальности и года нет.");

            var groupIds = groups.Select(g => g.GroupID).ToList();

            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");

            var studentsData = await _context.Students
                .Where(s => groupIds.Contains(s.GroupID))
                .OrderBy(s => s.LastName)
                .Select(s => new
                {
                    Student = new { s.StudentID, s.Name, s.Surname, s.LastName, s.Group },
                    Marks = s.Marks
                        .Where(m =>
                            m.FlagF == 0 &&
                            m.TypeOfExerciseID != typeKP.TypeOfExerciseID &&
                            m.TypeOfExerciseID != typeKR.TypeOfExerciseID
                        )
                        .Select(m => new { m.SubjectID, m.Value })
                })
                .AsNoTracking()
                .ToListAsync();

            if (!studentsData.Any())
                return NotFound("Студентов в группе нет.");

            var subjectIds = studentsData
                .SelectMany(s => s.Marks)
                .Select(m => m.SubjectID)
                .Distinct()
                .ToList();

            var allSubjects = await _context.Subjects
                .Where(sub => subjectIds.Contains(sub.SubjectID))
                .Select(sub => new { sub.SubjectID, sub.ShortName, sub.Name })
                .ToListAsync();

            var students = studentsData.Select(s => new CourseStudentViewModel
            {
                StudentId = s.Student.StudentID,
                Name = s.Student.Name,
                Surname = s.Student.Surname,
                LastName = s.Student.LastName,
                GroupID = s.Student.Group.GroupID,
                GroupName = s.Student.Group.Name,

                SubjectAverages = allSubjects.Select(subject =>
                {
                    var marks = s.Marks
                        .Where(m => m.SubjectID == subject.SubjectID)
                        .Select(m => double.TryParse(m.Value, out var v) ? (double?)v : null)
                        .Where(v => v.HasValue)
                        .Select(v => v.Value)
                        .ToList();

                    return new SubjectAverageViewModel
                    {
                        SubjectId = subject.SubjectID,
                        SubjectName = subject.ShortName,
                        SubjectFullName = subject.Name,
                        AvgMark = marks.Any() ? marks.Average() : (double?)null
                    };
                }).ToList()
            }).ToList();

            ViewBag.SpecialityName = speciality.Name;
            ViewBag.SpecialityID = id;
            ViewBag.Year = year;

            return View(students);
        }

        public async Task<IActionResult> ExportCourseStatement(int id, int year)
        {
            var students = await GetCourseStatementViewModel(id, year);
            if (students == null || !students.Any())
                return NotFound("Студентов в группе нет.");

            // Генерируем Excel
            var content = GenerateExcelFile(students, $"Ведомость_{id}");

            // Возвращаем файл
            return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Ведомость_{id}.xlsx");
        }

        // Приватный метод генерации Excel
        private byte[] GenerateExcelFile(List<CourseStudentViewModel> students, string fileName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ведомость");

            int col = 1;
            worksheet.Cell(1, col++).Value = "#";
            worksheet.Cell(1, col++).Value = "Ф.И.О.";
            worksheet.Cell(1, col++).Value = "Группа";

            var subjects = students.First().SubjectAverages;
            foreach (var subject in subjects)
            {
                var cell = worksheet.Cell(1, col++);
                cell.Value = subject.SubjectName;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.TextRotation = 90; // поворот текста
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            int row = 2;
            int studentNumber = 1;
            foreach (var student in students)
            {
                col = 1;
                worksheet.Cell(row, col++).Value = studentNumber++;
                worksheet.Cell(row, col++).Value = $"{student.LastName} {student.Name} {student.Surname}";
                worksheet.Cell(row, col++).Value = student.GroupName;


                foreach (var subj in student.SubjectAverages)
                {
                    var cell = worksheet.Cell(row, col++);
                    if (subj.AvgMark.HasValue)
                    {
                        cell.Value = subj.AvgMark.Value;
                        cell.Style.NumberFormat.Format = "0.000"; // до тысячных
                    }
                    else
                    {
                        cell.Value = "-";
                    }

                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // по центру
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                row++;
            }

            // Форматирование заголовка
            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.Gray;
            worksheet.Row(1).Height = 80; // под повёрнутый текст

            // Фиксируем первую строку при скролле
            worksheet.SheetView.FreezeRows(1);

            worksheet.Columns().AdjustToContents();

            // --- Добавляем границы таблицы ---
            var usedRange = worksheet.RangeUsed();
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // Вспомогательный метод для получения ViewModel (можно вынести отдельно)
        private async Task<List<CourseStudentViewModel>> GetCourseStatementViewModel(int id, int year)
        {
            var groupIds = await _context.Groups
                .Where(g => g.SpecialityID == id && g.DateEnter.Year == year)
                .Select(g => g.GroupID)
                .ToListAsync();

            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");

            var studentsData = await _context.Students
                .Where(s => groupIds.Contains(s.GroupID))
                .OrderBy(s => s.LastName)
                .Select(s => new
                {
                    Student = new { s.StudentID, s.Name, s.Surname, s.LastName, s.Group },
                    Marks = s.Marks
                        .Where(m =>
                            m.FlagF == 0 &&
                            m.TypeOfExerciseID != typeKP.TypeOfExerciseID &&
                            m.TypeOfExerciseID != typeKR.TypeOfExerciseID
                        )
                        .Select(m => new { m.SubjectID, m.Value })
                        .ToList()
                })
                .AsNoTracking()
                .ToListAsync();

            if (!studentsData.Any())
                return null;

            var subjectIds = studentsData.SelectMany(s => s.Marks)
                                         .Select(m => m.SubjectID)
                                         .Distinct()
                                         .ToList();

            var allSubjects = await _context.Subjects
                .Where(sub => subjectIds.Contains(sub.SubjectID))
                .Select(sub => new { sub.SubjectID, sub.Name })
                .ToListAsync();

            var students = studentsData.Select(s => new CourseStudentViewModel
            {
                StudentId = s.Student.StudentID,
                Name = s.Student.Name,
                Surname = s.Student.Surname,
                LastName = s.Student.LastName,
                GroupID = s.Student.Group.GroupID,
                GroupName = s.Student.Group.Name,

                SubjectAverages = allSubjects.Select(subject =>
                {
                    var marks = s.Marks
                        .Where(m => m.SubjectID == subject.SubjectID)
                        .Select(m => double.TryParse(m.Value, out var v) ? (double?)v : null)
                        .Where(v => v.HasValue)
                        .Select(v => v.Value)
                        .ToList();

                    return new SubjectAverageViewModel
                    {
                        SubjectId = subject.SubjectID,
                        SubjectName = subject.Name,
                        AvgMark = marks.Any() ? marks.Average() : (double?)null
                    };
                }).ToList()
            }).ToList();

            return students;
        }

        [Authorize]
        public async Task<IActionResult> CourseSummaryStatement(int id, DateTime date_1, DateTime date_2, int year)
        {
            var speciality = await _context.Specialities.FindAsync(id);

            var groupIds = await _context.Groups
                .Where(g => g.SpecialityID == id && g.DateEnter.Year == year)
                .Select(g => g.GroupID)
                .ToListAsync();

            // Типы, которые нужны из Marks (пример: Итоговая, Курсовая работа, Курсовой проект)
            var typeIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая отметка");
            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");

            var validTypeIds = new HashSet<int>(
                new[] { typeIO?.TypeOfExerciseID, typeKR?.TypeOfExerciseID, typeKP?.TypeOfExerciseID }
                .Where(x => x.HasValue).Select(x => x.Value)
            );

            // Студенты
            var students = await _context.Students
                .Where(s => groupIds.Contains(s.GroupID) && s.Status == true)
                .OrderBy(s => s.LastName)
                .Select(s => new GroupSummaryStatementViewModel
                {
                    StudentID = s.StudentID,
                    GroupID = s.GroupID,
                    StudentName = s.Name,
                    StudentLastName = s.LastName,
                    StudentSurname = s.Surname,
                    Marks = new List<MarkGroupSummaryStatement>()
                })
                .AsNoTracking()
                .ToListAsync();

            if (students.Count == 0) return View(students);

            var studentIds = students.Select(st => st.StudentID).ToList();

            // ЕДИНАЯ выборка оценок из двух таблиц без навигаций
            var allMarks = await (
                // 1) Оценки из Marks — только нужные типы
                from m in _context.Marks
                where studentIds.Contains(m.StudentID) && validTypeIds.Contains(m.TypeOfExerciseID)
                join sub in _context.Subjects on m.SubjectID equals sub.SubjectID into subg
                from sub in subg.DefaultIfEmpty()
                join tp in _context.Types on m.TypeOfExerciseID equals tp.TypeOfExerciseID into tpg
                from tp in tpg.DefaultIfEmpty()
                select new
                {
                    m.StudentID,
                    m.Date,
                    m.Value,
                    SubjectID = (int?)m.SubjectID,
                    SubjectName = sub != null ? sub.Name : null,
                    ShortSubjectName = sub != null ? sub.ShortName : null,
                    TypeID = m.TypeOfExerciseID,
                    TypeName = tp != null ? tp.Name : null,
                    ShortTypeName = tp != null ? tp.ShortName : null
                }
            )
            // 2) Плюс оценки из StatementMarks (если надо — тоже можно отфильтровать по типам)
            .Concat(
                from sm in _context.StatementMarks
                where studentIds.Contains(sm.StudentID)
                join tp in _context.Types on sm.TypeOfExerciseID equals tp.TypeOfExerciseID into tpg2
                from tp in tpg2.DefaultIfEmpty()
                select new
                {
                    sm.StudentID,
                    sm.Date,
                    sm.Value,
                    SubjectID = (int?)null,
                    SubjectName = (string)null,
                    ShortSubjectName = (string)null,
                    TypeID = sm.TypeOfExerciseID,
                    TypeName = tp != null ? tp.Name : null,
                    ShortTypeName = tp != null ? tp.ShortName : null
                }
            )
            .AsNoTracking()
            .ToListAsync();

            if (date_1.ToShortDateString() != "01.01.0001")
            {
                allMarks = allMarks
                    .Where(m => m.Date >= date_1)
                    .ToList();
            }

            if (date_2.ToShortDateString() != "01.01.0001")
            {
                allMarks = allMarks
                    .Where(m => m.Date <= date_2)
                    .ToList();
            }

            // Группируем по студенту и наполняем VM
            var marksByStudent = allMarks
                .GroupBy(x => x.StudentID)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.SubjectID)
                          .Select(x => new MarkGroupSummaryStatement
                          {
                              Value = x.Value,
                              Date = x.Date,
                              SubjectID = x.SubjectID,
                              SubjectName = x.SubjectName,
                              ShortSubjectName = x.ShortSubjectName,
                              TypeID = x.TypeID,
                              TypeName = x.TypeName,
                              ShortTypeName = x.ShortTypeName
                          })
                          .ToList()
                );

            foreach (var s in students)
                if (marksByStudent.TryGetValue(s.StudentID, out var list))
                    s.Marks = list;

            ViewBag.Speciality = speciality;
            ViewBag.Year = year;
            ViewBag.Date_1 = date_1;
            ViewBag.Date_2 = date_2;

            return View(students);
        }

        [Authorize]
        public async Task<IActionResult> ExportCourseSummaryStatement(int id, int year, string date_1, string date_2)
        {
            var speciality = await _context.Specialities.FindAsync(id);

            // Группы по специальности и году
            var groupIds = await _context.Groups
                .Where(g => g.SpecialityID == id && g.DateEnter.Year == year)
                .Select(g => g.GroupID)
                .ToListAsync();

            var students = await _context.Students
                .Where(s => groupIds.Contains(s.GroupID) && s.Status == true)
                .OrderBy(s => s.LastName)
                .Select(s => new
                {
                    s.StudentID,
                    s.LastName,
                    s.Name,
                    s.Surname
                })
                .AsNoTracking()
                .ToListAsync();

            if (students.Count == 0)
                return Content("Нет данных для экспорта");

            // Типы
            var typeIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая отметка");
            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");

            var validTypeIds = new HashSet<int>(
                new[] { typeIO?.TypeOfExerciseID, typeKR?.TypeOfExerciseID, typeKP?.TypeOfExerciseID }
                .Where(x => x.HasValue).Select(x => x.Value)
            );

            var studentIds = students.Select(s => s.StudentID).ToList();

            // --- Оценки ---
            var marksFromMarks = from m in _context.Marks
                                 where studentIds.Contains(m.StudentID) && validTypeIds.Contains(m.TypeOfExerciseID)
                                 join sub in _context.Subjects on m.SubjectID equals sub.SubjectID into subg
                                 from sub in subg.DefaultIfEmpty()
                                 join tp in _context.Types on m.TypeOfExerciseID equals tp.TypeOfExerciseID into tpg
                                 from tp in tpg.DefaultIfEmpty()
                                 select new
                                 {
                                     m.StudentID,
                                     m.Date,
                                     m.Value,
                                     SubjectName = sub != null ? sub.Name : "Без предмета",
                                     TypeName = tp != null ? tp.Name : "Неизвестный тип"
                                 };

            var marksFromStatement = from sm in _context.StatementMarks
                                     where studentIds.Contains(sm.StudentID)
                                     join tp in _context.Types on sm.TypeOfExerciseID equals tp.TypeOfExerciseID into tpg2
                                     from tp in tpg2.DefaultIfEmpty()
                                     select new
                                     {
                                         sm.StudentID,
                                         sm.Date,
                                         sm.Value,
                                         SubjectName = "Без предмета",
                                         TypeName = tp != null ? tp.Name : "Неизвестный тип"
                                     };

            var allMarks = (await marksFromMarks.Concat(marksFromStatement).AsNoTracking().ToListAsync())
                .OrderBy(m => m.StudentID)
                .ThenBy(m => m.SubjectName)
                .ThenBy(m => m.TypeName)
                .ThenBy(m => m.Date)
                .ToList();

            DateTime? d1 = TryParse(date_1);
            DateTime? d2 = TryParse(date_2);

            if (d1.HasValue && d1.Value != DateTime.MinValue)
            {
                allMarks = allMarks
                    .Where(m => m.Date >= d1)
                    .ToList();
            }

            if (d2.HasValue && d2.Value != DateTime.MinValue)
            {
                allMarks = allMarks
                    .Where(m => m.Date <= d2)
                    .ToList();
            }

            // --- Все комбинации предмет + тип + номер попытки ---
            var subjectTypePairs = allMarks
                .GroupBy(m => new { m.SubjectName, m.TypeName, m.StudentID })
                .SelectMany(g => g.OrderBy(m => m.Date)
                                  .Select((m, idx) => new { g.Key.SubjectName, g.Key.TypeName, Attempt = idx + 1 }))
                .Distinct()
                .ToList();

            if (!subjectTypePairs.Any())
                subjectTypePairs.Add(new { SubjectName = "Без предмета", TypeName = "Нет типов", Attempt = 1 });

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Сводная ведомость");

            ws.Cell(1, 1).Value = $"Сводная ведомость: {speciality?.Name ?? "—"} ({year})";
            ws.Range(1, 1, 1, 4 + subjectTypePairs.Count).Merge()
                .Style.Font.SetBold().Font.SetFontSize(14);

            int subjectRow = 3;
            int typeRow = 4;

            // Левые колонки
            var leftHeaders = new[] { "№", "Фамилия", "Имя", "Отчество" };
            for (int i = 0; i < leftHeaders.Length; i++)
            {
                int col = i + 1;
                ws.Range(subjectRow, col, typeRow, col).Merge();
                ws.Cell(subjectRow, col).Value = leftHeaders[i];
                ws.Cell(subjectRow, col).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(subjectRow, col).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                ws.Cell(subjectRow, col).Style.Font.SetBold();
            }

            // Шапка: предметы и типы (с учётом попыток)
            int colIndex = 5;
            var subjectsGroups = subjectTypePairs.GroupBy(p => p.SubjectName).ToList();

            foreach (var subjGroup in subjectsGroups)
            {
                int fromCol = colIndex;
                int span = subjGroup.Count();
                int toCol = colIndex + span - 1;

                // объединяем заголовок для предмета
                ws.Range(subjectRow, fromCol, subjectRow, toCol).Merge();
                ws.Cell(subjectRow, fromCol).Value = subjGroup.Key;
                ws.Cell(subjectRow, fromCol).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(subjectRow, fromCol).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                ws.Cell(subjectRow, fromCol).Style.Font.SetBold();

                foreach (var pair in subjGroup)
                {
                    var cell = ws.Cell(typeRow, colIndex);
                    cell.Value = $"{pair.TypeName}-{pair.Attempt}";
                    cell.Style.Alignment.TextRotation = 90;
                    cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                    cell.Style.Font.SetBold();
                    ws.Column(colIndex).Width = 6;
                    colIndex++;
                }
            }

            ws.Row(typeRow).Height = 120;
            ws.Row(subjectRow).Height = 25;

            // Данные студентов
            int dataStartRow = typeRow + 1;
            int row = dataStartRow;
            int idx = 1;

            foreach (var st in students)
            {
                ws.Cell(row, 1).Value = idx;
                ws.Cell(row, 2).Value = st.LastName;
                ws.Cell(row, 3).Value = st.Name;
                ws.Cell(row, 4).Value = st.Surname;

                int c = 5;
                foreach (var pair in subjectTypePairs)
                {
                    var val = allMarks
                        .Where(m => m.StudentID == st.StudentID
                                 && m.SubjectName == pair.SubjectName
                                 && m.TypeName == pair.TypeName)
                        .OrderBy(m => m.Date)
                        .Skip(pair.Attempt - 1)
                        .Select(m => m.Value)
                        .FirstOrDefault();

                    ws.Cell(row, c).Value = val ?? "";
                    ws.Cell(row, c).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws.Cell(row, c).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                    c++;
                }

                row++;
                idx++;
            }

            ws.Columns(1, 4).AdjustToContents();
            ws.SheetView.FreezeRows(dataStartRow - 1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            string fileName = $"Сводная_ведомость_{speciality?.Name ?? "spec"}_{year}.xlsx";

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [Authorize]
        public async Task<IActionResult> StatementTable(int id, int year)
        {
            // Получаем группы по специальности и году
            var groups = await _context.Groups
                .Where(g => g.SpecialityID == id && g.DateEnter.Year == year)
                .ToListAsync();

            var groupIds = groups.Select(g => g.GroupID).ToList();

            // Получаем дисциплины для этих групп через журнал
            var subjectIds = await _context.Journals
                .Where(j => groupIds.Contains(j.GroupID))
                .Select(j => j.SubjectID)
                .ToListAsync();

            var disciplines = await _context.Subjects
                .Where(s => subjectIds.Contains(s.SubjectID))
                .OrderBy(s => s.Name)
                .ToListAsync();

            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");

            var speciality = await _context.Specialities.FindAsync(id);

            var courseGroups = new List<CourseGroup>();

            foreach (var group in groups)
            {
                // Получаем дисциплины этой группы
                var subjects = await _context.Subjects
                    .Where(s => _context.Journals.Any(j => j.GroupID == group.GroupID && j.SubjectID == s.SubjectID))
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var courseDisciplines = new List<CourseDiscipline>();

                foreach (var subject in subjects)
                {
                    var students = await _context.Students
                        .Where(st => st.GroupID == group.GroupID)
                        .ToListAsync();

                    var studentAverages = new List<double>();

                    foreach (var student in students)
                    {
                        var marks = await _context.Marks
                            .Where(m =>
                                m.StudentID == student.StudentID &&
                                m.SubjectID == subject.SubjectID &&
                                m.FlagF == 0 &&
                                m.TypeOfExerciseID != typeKP.TypeOfExerciseID &&
                                m.TypeOfExerciseID != typeKR.TypeOfExerciseID
                            )
                            .Select(m => m.Value)
                            .ToListAsync();

                        var numericMarks = marks
                            .Select(m => double.TryParse(m, out var d) ? d : (double?)null)
                            .Where(d => d.HasValue)
                            .Select(d => d.Value)
                            .ToList();

                        if (numericMarks.Any())
                        {
                            studentAverages.Add(numericMarks.Average());
                        }
                    }

                    double groupDisciplineAverage = 0;
                    if (studentAverages.Any())
                    {
                        // Математическое округление до тысячных
                        groupDisciplineAverage = Math.Round(studentAverages.Average(), 3, MidpointRounding.AwayFromZero);
                    }

                    courseDisciplines.Add(new CourseDiscipline
                    {
                        Discipline = subject,
                        Mark = groupDisciplineAverage
                    });
                }

                courseGroups.Add(new CourseGroup
                {
                    Group = group,
                    CourseDisciplines = courseDisciplines
                });
            }

            ViewBag.Disciplines = disciplines;
            ViewBag.Speciality = speciality;
            ViewBag.Year = year;

            return View(courseGroups);
        }

        private DateTime? TryParse(string input)
        {
            if (DateTime.TryParse(input, out var dt))
                return dt;
            return null;
        }

        private bool SpecialityExists(int id)
        {
            return _context.Specialities.Any(e => e.SpecialityID == id);
        }
    }
}