using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
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

            var typeIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая отметка");
            if (typeIO == null)
                return NotFound("Тип 'Итоговая отметка' не найден.");

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
                                ? Math.Round(numericMarks.Average(), 3, MidpointRounding.AwayFromZero).ToString()
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

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Institutes
                .OrderBy(i => i.Arch)
                    .ThenBy(i => i.Name)
                .ToListAsync());
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH, ANB-HEAD, ANB-CI, ANB-ICDA, ANB-IST, ANB-SFIPKIP, User")]
        public async Task<IActionResult> Details(int? id)
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

            // Получаем типы занятий
            var typeSZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Семинарское занятие");
            var typePZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Практическое занятие");
            var typeLZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лабораторное занятие");
            var typeL = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лекция");
            var typeKM = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Контрольное мероприятие");
            var typeGPZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Городское практическое занятие");

            List<double> marksAverage = new();
            List<Mark> marks = new();
            string term;
            var date = DateTime.Now.AddYears(-1);

            // Получаем ВСЕ отметки для института
            var allMarks = await _context.Marks
                .Where(m => m.InstituteID == id)
                .ToListAsync();

            // ========================================
            // ФИЛЬТРУЕМ ОТМЕТКИ В ЗАВИСИМОСТИ ОТ СЕМЕСТРА
            // ========================================

            // Сентябрь-Декабрь - первый семестр
            if (DateTime.Now.Month >= 9 && DateTime.Now.Month <= 12)
            {
                term = "первый семестр";

                // Отметки за сентябрь-декабрь текущего года
                var semesterMarks = allMarks
                    .Where(m =>
                        (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                        m.Date.Year == DateTime.Now.Year &&
                        (m.Date.Month >= 9 && m.Date.Month <= 12)
                    )
                    .ToList();

                // Добавляем незавершенную аттестацию
                var incompleteMarks = allMarks
                    .Where(m => m.FlagF == 0)
                    .ToList();

                // Объединяем и убираем дубликаты
                var allIds = new HashSet<int>();
                marks = semesterMarks
                    .Concat(incompleteMarks)
                    .Where(m => allIds.Add(m.MarkID))
                    .ToList();
            }
            // Январь - первый семестр (добираем отметки)
            else if (DateTime.Now.Month == 1)
            {
                term = "первый семестр";

                // Отметки за сентябрь-декабрь прошлого года + январь текущего
                var semesterMarks = allMarks
                    .Where(m =>
                        (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                        ((m.Date.Year == DateTime.Now.Year && m.Date.Month == 1) ||
                         (m.Date.Year == date.Year && m.Date.Month >= 9 && m.Date.Month <= 12))
                    )
                    .ToList();

                // Добавляем незавершенную аттестацию
                var incompleteMarks = allMarks
                    .Where(m => m.FlagF == 0)
                    .ToList();

                // Объединяем и убираем дубликаты
                var allIds = new HashSet<int>();
                marks = semesterMarks
                    .Concat(incompleteMarks)
                    .Where(m => allIds.Add(m.MarkID))
                    .ToList();
            }
            // Февраль-Август - второй семестр
            else
            {
                term = "второй семестр";

                // Отметки за февраль-август текущего года
                var semesterMarks = allMarks
                    .Where(m =>
                        (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                        m.Date.Year == DateTime.Now.Year &&
                        (m.Date.Month >= 2 && m.Date.Month <= 8)
                    )
                    .ToList();

                // Добавляем незавершенную аттестацию
                var incompleteMarks = allMarks
                    .Where(m => m.FlagF == 0)
                    .ToList();

                // Объединяем и убираем дубликаты
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
            // СТУДЕНТЫ
            // ========================================
            var students = await _context.Students
                .Include(s => s.Group)
                .Where(s => s.InstituteID == id && s.Status == true)
                .AsNoTracking()
                .ToListAsync();
            int studentNumber = students.Count;

            // ========================================
            // РЕЙТИНГ СТУДЕНТОВ
            // ========================================
            List<StudentRaiting> studentRaitings = new();
            foreach (var student in students)
            {
                double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, marks);

                StudentRaiting sr = new StudentRaiting()
                {
                    Group = student.Group,
                    Student = student,
                    Raiting = studentRating ?? 0,
                };

                studentRaitings.Add(sr);
            }

            // ========================================
            // ГРУППЫ
            // ========================================
            var groups = await _context.Groups
                .Where(g => g.InstituteID == id && g.DateExit > DateTime.Now)
                .ToListAsync();

            // ========================================
            // СПЕЦИАЛЬНОСТИ
            // ========================================
            var specialitiesCount = await _context.Specialities
                .CountAsync(s => s.InstituteID == id && s.Arch == false);

            // ========================================
            // РЕЙТИНГ ГРУПП
            // ========================================
            List<InstGroupRaiting> groupsRating = new();
            foreach (var group in groups)
            {
                var groupStudents = students
                    .Where(s => s.GroupID == group.GroupID && s.Status == true)
                    .ToList();

                var groupMarks = marks
                    .Where(m => m.GroupID == group.GroupID)
                    .ToList();

                List<double> groupStudentRatings = new();
                foreach (var student in groupStudents)
                {
                    double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, groupMarks);
                    if (studentRating != null && studentRating > 0)
                    {
                        groupStudentRatings.Add((double)studentRating);
                    }
                }

                double groupRating = 0;
                if (groupStudentRatings.Any())
                {
                    groupRating = Math.Round(groupStudentRatings.Average(), 3, MidpointRounding.AwayFromZero);
                }

                InstGroupRaiting instGroupRaiting = new()
                {
                    Group = group,
                    Raiting = groupRating
                };

                groupsRating.Add(instGroupRaiting);
            }

            // ========================================
            // СРЕДНИЙ БАЛЛ ИНСТИТУТА
            // ========================================
            double instRating = 0;
            var filterGroupsRatig = groupsRating.Where(m => m.Raiting != 0).ToList();
            if (filterGroupsRatig.Any())
            {
                instRating = Math.Round(filterGroupsRatig.Average(g => g.Raiting), 3, MidpointRounding.AwayFromZero);
            }

            // ========================================
            // РАСПРЕДЕЛЕНИЕ ОТМЕТОК (1-10)
            // ========================================
            Dictionary<int, int> marksNumber = new();
            Dictionary<int, decimal> marksPercent = new();
            for (int i = 1; i <= 10; i++)
            {
                int count = marksAverage.Where(x => (int)x == i).Count();
                marksNumber.Add(i, count);

                if (marksAverage.Count > 0)
                {
                    decimal percent = (decimal)count / marksAverage.Count * 100;
                    marksPercent.Add(i, Math.Round(percent, 3, MidpointRounding.AwayFromZero));
                }
                else
                {
                    marksPercent.Add(i, 0);
                }
            }

            // ========================================
            // МЕСЯЧНАЯ УСПЕВАЕМОСТЬ
            // ========================================
            Dictionary<string, string> raitingTime = new();

            // Сентябрь
            var sepMarks = marks.Where(m => m.Date.Month == 9).ToList();
            if (sepMarks.Any())
            {
                double avg = await GetAverageMarkAsync(sepMarks);
                raitingTime.Add("Сентябрь", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Сентябрь", "0");
            }

            // Октябрь
            var octMarks = marks.Where(m => m.Date.Month == 10).ToList();
            if (octMarks.Any())
            {
                double avg = await GetAverageMarkAsync(octMarks);
                raitingTime.Add("Октябрь", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Октябрь", "0");
            }

            // Ноябрь
            var novMarks = marks.Where(m => m.Date.Month == 11).ToList();
            if (novMarks.Any())
            {
                double avg = await GetAverageMarkAsync(novMarks);
                raitingTime.Add("Ноябрь", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Ноябрь", "0");
            }

            // Декабрь
            var decMarks = marks.Where(m => m.Date.Month == 12).ToList();
            if (decMarks.Any())
            {
                double avg = await GetAverageMarkAsync(decMarks);
                raitingTime.Add("Декабрь", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Декабрь", "0");
            }

            // Январь
            var janMarks = marks.Where(m => m.Date.Month == 1).ToList();
            if (janMarks.Any())
            {
                double avg = await GetAverageMarkAsync(janMarks);
                raitingTime.Add("Январь", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Январь", "0");
            }

            // Февраль
            var febMarks = marks.Where(m => m.Date.Month == 2).ToList();
            if (febMarks.Any())
            {
                double avg = await GetAverageMarkAsync(febMarks);
                raitingTime.Add("Февраль", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Февраль", "0");
            }

            // Март
            var marMarks = marks.Where(m => m.Date.Month == 3).ToList();
            if (marMarks.Any())
            {
                double avg = await GetAverageMarkAsync(marMarks);
                raitingTime.Add("Март", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Март", "0");
            }

            // Апрель
            var aprMarks = marks.Where(m => m.Date.Month == 4).ToList();
            if (aprMarks.Any())
            {
                double avg = await GetAverageMarkAsync(aprMarks);
                raitingTime.Add("Апрель", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Апрель", "0");
            }

            // Май
            var mayMarks = marks.Where(m => m.Date.Month == 5).ToList();
            if (mayMarks.Any())
            {
                double avg = await GetAverageMarkAsync(mayMarks);
                raitingTime.Add("Май", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Май", "0");
            }

            // Июнь
            var junMarks = marks.Where(m => m.Date.Month == 6).ToList();
            if (junMarks.Any())
            {
                double avg = await GetAverageMarkAsync(junMarks);
                raitingTime.Add("Июнь", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Июнь", "0");
            }

            // Июль
            var julMarks = marks.Where(m => m.Date.Month == 7).ToList();
            if (julMarks.Any())
            {
                double avg = await GetAverageMarkAsync(julMarks);
                raitingTime.Add("Июль", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Июль", "0");
            }

            // Август
            var augMarks = marks.Where(m => m.Date.Month == 8).ToList();
            if (augMarks.Any())
            {
                double avg = await GetAverageMarkAsync(augMarks);
                raitingTime.Add("Август", avg.ToString("0.000").Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Август", "0");
            }

            // ========================================
            // УЧЕБНЫЙ ГОД
            // ========================================
            string yearsStudy = "";
            if (DateTime.Now.Month >= 9 && DateTime.Now.Month <= 12)
            {
                yearsStudy = $"{DateTime.Now.Year}/{DateTime.Now.Year + 1}";
            }
            else
            {
                yearsStudy = $"{DateTime.Now.Year - 1}/{DateTime.Now.Year}";
            }

            // ========================================
            // ЛУЧШИЙ И ХУДШИЙ СТУДЕНТ
            // ========================================
            var bestStudent = studentRaitings.OrderByDescending(s => s.Raiting).FirstOrDefault();
            if (bestStudent == null)
            {
                ViewBag.BestStudent = new StudentRaiting { Raiting = 0 };
            }
            else
            {
                ViewBag.BestStudent = bestStudent;
            }

            var worstStudent = studentRaitings.OrderByDescending(s => s.Raiting).LastOrDefault();
            if (worstStudent == null)
            {
                ViewBag.WorseStudent = new StudentRaiting { Raiting = 0 };
            }
            else
            {
                ViewBag.WorseStudent = worstStudent;
            }

            // ========================================
            // ЛУЧШАЯ И ХУДШАЯ ГРУППА
            // ========================================
            var bestGroup = groupsRating.OrderByDescending(g => g.Raiting).FirstOrDefault();
            if (bestGroup == null)
            {
                ViewBag.BestGroup = new InstGroupRaiting { Raiting = 0 };
            }
            else
            {
                ViewBag.BestGroup = bestGroup;
            }

            var worstGroup = groupsRating.OrderByDescending(g => g.Raiting).LastOrDefault();
            if (worstGroup == null)
            {
                ViewBag.WorseGroup = new InstGroupRaiting { Raiting = 0 };
            }
            else
            {
                ViewBag.WorseGroup = worstGroup;
            }

            // ========================================
            // VIEWBAG
            // ========================================
            ViewBag.Term = term;
            ViewBag.Raiting = Math.Round(instRating, 3, MidpointRounding.AwayFromZero);
            ViewBag.Students = studentNumber;
            ViewBag.Groups = groups.Count();
            ViewBag.Specialities = specialitiesCount;
            ViewBag.StudentsRaiting = studentRaitings.OrderByDescending(s => s.Raiting);
            ViewBag.GroupsRaiting = groupsRating.OrderByDescending(g => g.Raiting);
            ViewBag.MarksNumber = marksNumber;
            ViewBag.MarksPercent = marksPercent;
            ViewBag.TimeRaiting = raitingTime;
            ViewBag.Year = yearsStudy;

            return View(institute);
        }

        // Вспомогательный метод для подсчета среднего балла за месяц
        private async Task<double> GetAverageMarkAsync(List<Mark> marks)
        {
            var numericMarks = marks
                .Select(m => double.TryParse(m.Value, out var value) ? value : (double?)null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            if (numericMarks.Any())
            {
                return Math.Round(numericMarks.Average(), 3, MidpointRounding.AwayFromZero);
            }

            return 0;
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
