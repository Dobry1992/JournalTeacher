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
           
            //Текущий средний балл
            var date = DateTime.Now.AddYears(-1);
            var typeSZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Семинарское занятие");
            var typePZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Практическое занятие");
            var typeLZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лабораторное занятие");
            var typeL = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лекция");
            var typeKM = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Контрольное мероприятие");
            var typeGPZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Городское практическое занятие");

            var typeEKZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Экзамен");
            var typeDZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дифференцированный зачёт");
            var typeZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Зачёт");
            var typeF = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая оценка");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            List<double> marksAverage = new();
            List<Mark> marks = new();

            //выборка оценок
            if (DateTime.Now.Month.ToString() == "9" || DateTime.Now.Month.ToString() == "10" || DateTime.Now.Month.ToString() == "11" || DateTime.Now.Month.ToString() == "12")
            {
                marks = student.Marks
                    .Where(m =>
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
                    .ToList();
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
                marks = student.Marks
                    .Where(m =>
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
                    .ToList();
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
                marks = student.Marks
                    .Where(m =>
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
                    .ToList();
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

            double raiting = (double)_studentAverageMarkService.GetStudentAverageMark(student, marks);

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

            //Оценочные показатели слушателя/курсанта
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

            //Оценка посещаемости
            int b = 0, nr = 0, o = 0, km = 0, r = 0, nb = 0, num = 0;
            double bp = 0, nrp = 0, op = 0, kmp = 0, rp = 0, nbp = 0, nump = 0;
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
            Dictionary<string, int> attendanceNumber = new Dictionary<string, int>()
            {
                {"Болезнь", b},
                {"Наряд", nr},
                {"Отпуск", o},
                {"Коммандировка", km},
                {"Отсутствие по мотивированный рапорт", r},
                {"Отсутствие без уважительной причины", nb},
                {"Присутствие", num}
            };
            bp = ((double)b / mNumber) * 100;
            nrp = ((double)nr / mNumber) * 100;
            op = ((double)o / mNumber) * 100;
            kmp = ((double)km / mNumber) * 100;
            rp = ((double)r / mNumber) * 100;
            nbp = ((double)nb / mNumber) * 100;
            nump = ((double)num / mNumber) * 100;
            Dictionary<string, double> attendancePercent = new Dictionary<string, double>()
            {
                {"Болезнь", Math.Round(bp, 3)},
                {"Наряд", Math.Round(nrp, 3)},
                {"Отпуск", Math.Round(op, 3) },
                {"Коммандировка", Math.Round(kmp, 3)},
                {"Отсутствие по мотивированный рапорт", Math.Round(rp, 3)},
                {"Отсутствие без уважительной причины", Math.Round(nbp, 3)},
                {"Присутствие", Math.Round(nump, 3)}
            };

            //Текущий общий средний балл и средний бал за предмет по месяцам
            int subjectID = 0;
            if (!String.IsNullOrEmpty(searchString))
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
            var subject = await _context.Subjects
                .FindAsync(subjectID);
            var subjectMarks = marks
                .Where(m => m.SubjectID == subjectID)
                .ToList();
            Dictionary<string, string> raitingTimeSubject = new();
            Dictionary<string, string> raitingTime = new();
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };

            foreach (string month in months)
            {
                int monthNumber = Array.IndexOf(months, month) + 1;
                var timeMarks = marks
                    .Where(m => m.Date.Month.ToString() == monthNumber.ToString())
                    .ToList();
                raitingTime.Add(month, _studentAverageMarkService.GetStudentAverageMark(student, timeMarks).ToString().Replace(",", "."));
                var monthMarks = subjectMarks
                    .Where(m => m.Date.Month.ToString() == monthNumber.ToString())
                    .ToList();
                List<double> doubleMarks = new List<double>();
                foreach (var mark in monthMarks)
                {
                    if (double.TryParse(mark.Value, out var number))
                    {
                        doubleMarks.Add(number);
                    }
                }

                if (doubleMarks.Any())
                {
                    raitingTimeSubject.Add(month, Math.Round(doubleMarks.Average(), 3).ToString().Replace(",", "."));
                }
                else
                {
                    raitingTimeSubject.Add(month, "0");
                }
            }
            //конец блока

            //Диаграмма предметов, роза ветров
            var journals = student.Group.Journals.ToList();
            List<object> radar = new();
            foreach (var journal in journals)
            {
                List<double> val = new();
                var studentSubjectMarks = marks
                    .Where(m => 
                        m.SubjectID == journal.SubjectID &&
                        m.TypeOfExerciseID != typeEKZ.TypeOfExerciseID &&
                        m.TypeOfExerciseID != typeDZ.TypeOfExerciseID &&
                        m.TypeOfExerciseID != typeZ.TypeOfExerciseID &&
                        m.TypeOfExerciseID != typeF.TypeOfExerciseID &&
                        m.TypeOfExerciseID != typeKM.TypeOfExerciseID &&
                        m.TypeOfExerciseID != typeKP.TypeOfExerciseID
                    );
                foreach (var mark in studentSubjectMarks)
                {
                    if (double.TryParse(mark.Value, out var m))
                    {
                        val.Add(m);
                    }
                }

                if (val.Count != 0)
                {
                    double valRaiting = Math.Round(val.Average(), 3);
                    radar.Add(new { Subject = journal.Subject.ShortName.ToString(), Value = valRaiting.ToString().Replace(",", ".") });
                }
            }

            //Отрицательные результаты
            var negativeMarks = marks
                .Where(m => m.Value == "1" || m.Value == "2" || m.Value == "3")
                .ToList();

            //Итоговые результаты обучения
            var statementMarks = await _context.StatementMarks
                .Where(m => m.StudentID == id)
                .ToListAsync();
            List<FinalMark> finalMarks = new();
            foreach (var mark in statementMarks)
            {
                TypeOfExercise t = await _context.Types.FindAsync(mark.TypeOfExerciseID);
                FinalMark m = new();
                m.Mark = mark;
                m.Type = t;
                finalMarks.Add(m);
            }

            //Результаты обучения по предметам
            List<MarkSubjectFinal> markSubjectFinals = new();
            foreach (var journal in journals)
            {
                var mrks = student.Marks
                    .Where(m => m.SubjectID == journal.SubjectID &&
                        (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                        m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                        m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                        m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                        m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                        m.TypeOfExerciseID == typeL.TypeOfExerciseID)
                    );
                List<double> simplemrks = new();
                foreach (var m in mrks)
                {
                    if (double.TryParse(m.Value, out var vm))
                    {
                        simplemrks.Add(vm);
                    }
                }

                var controlMarks = student.Marks
                    .Where(m => m.SubjectID == journal.SubjectID && (m.TypeOfExerciseID == typeEKZ.TypeOfExerciseID || m.TypeOfExerciseID == typeDZ.TypeOfExerciseID || m.TypeOfExerciseID == typeZ.TypeOfExerciseID))
                    .ToList();
                List<Mark> controlmrks = new();
                foreach (var m in controlMarks)
                {
                    controlmrks.Add(m);
                }

                var fMarks = student.Marks
                    .Where(m => m.SubjectID == journal.SubjectID && m.TypeOfExerciseID == typeF.TypeOfExerciseID)
                    .ToList();
                List<Mark> fmrks = new();
                foreach (var m in fMarks)
                {
                    fmrks.Add(m);
                }

                var kMarks = student.Marks
                    .Where(m => m.SubjectID == journal.SubjectID && (m.TypeOfExerciseID == typeKP.TypeOfExerciseID || m.TypeOfExerciseID == typeKR.TypeOfExerciseID))
                    .ToList();
                List<Mark> kmarks = new();
                foreach (var m in kMarks)
                {
                    kmarks.Add(m);
                }

                MarkSubjectFinal msf = new();
                msf.Subject = journal.Subject;
                msf.Value = Math.Round(simplemrks.Sum() / simplemrks.Count, 2);
                msf.ControlMarks = controlmrks;
                msf.FinalMarks = fmrks;
                msf.ValueK = kmarks;
                markSubjectFinals.Add(msf);
            }

            StudentDetailsView studentDetailsView = new()
            {
                Student = student,
                AttendancePercent = attendancePercent,
                AttendanceNumber = attendanceNumber,
                MarkSubjectFinals = markSubjectFinals,
                FinalMarks = finalMarks
                    .OrderBy(m => m.Mark.Date)
                    .ToList(),
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
