using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Portal.Data;
using Portal.Models;
using Portal.ViewModel;

namespace Portal.Controllers
{
    public class StudentsController : Controller
    {
        private readonly AcademyContext _context;

        public StudentsController(AcademyContext context)
        {
            _context = context;
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
               .Include(s => s.Group)
                  .ThenInclude(g => g.Journals)
                       .ThenInclude(j => j.Subject)
               .Include(s => s.Group)
                  .ThenInclude(g => g.Speciality)
                       .ThenInclude(s => s.Institute)
               .Include(s => s.Marks)
               .FirstOrDefaultAsync(m => m.StudentID == id);

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

            List<double> marksAverage = new();
            IQueryable<Mark> marks;
            if (DateTime.Now.Month.ToString() == "9" || DateTime.Now.Month.ToString() == "10" || DateTime.Now.Month.ToString() == "11" || DateTime.Now.Month.ToString() == "12")
            {
                marks = _context.Marks
                    .Include(m => m.Theme)
                        .ThenInclude(t => t.Subject)
                    .Where(m => m.StudentID == id && (m.TypeOfExerciseID == typeKM.TypeOfExerciseID || m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID || m.TypeOfExerciseID == typeSZ.TypeOfExerciseID || m.TypeOfExerciseID == typePZ.TypeOfExerciseID || m.TypeOfExerciseID == typeLZ.TypeOfExerciseID || m.TypeOfExerciseID == typeL.TypeOfExerciseID) && m.Date.Year == DateTime.Now.Year
                    && (m.Date.Month.ToString() == "9" || m.Date.Month.ToString() == "10" || m.Date.Month.ToString() == "11" || m.Date.Month.ToString() == "12"));
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
                marks = _context.Marks
                    .Include(m => m.Theme)
                        .ThenInclude(t => t.Subject)
                    .Where(m => m.StudentID == id && (m.TypeOfExerciseID == typeKM.TypeOfExerciseID || m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID || m.TypeOfExerciseID == typeSZ.TypeOfExerciseID || m.TypeOfExerciseID == typePZ.TypeOfExerciseID || m.TypeOfExerciseID == typeLZ.TypeOfExerciseID || m.TypeOfExerciseID == typeL.TypeOfExerciseID) && ((m.Date.Year == DateTime.Now.Year && (m.Date.Month.ToString() == "1" || m.Date.Month.ToString() == "2" || m.Date.Month.ToString() == "3" || m.Date.Month.ToString() == "4" || m.Date.Month.ToString() == "5" || m.Date.Month.ToString() == "6" || m.Date.Month.ToString() == "7" || m.Date.Month.ToString() == "8") || m.Date.Year == date.Year && (m.Date.Month.ToString() == "9" || m.Date.Month.ToString() == "10" || m.Date.Month.ToString() == "11" || m.Date.Month.ToString() == "12"))));
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
            double raiting = marksAverage.Sum() / marksAverage.Count;

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

            //Текущий средний балл за предмет по месяцам
            int subjectID = 0;
            var subjects = _context.Subjects;
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
            var subject = await subjects.FindAsync(subjectID);
            var subjectMarks = marks.Where(m => m.SubjectID == subjectID);
            Dictionary<string, string> raitingTimeSubject = new();
            var septemberMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "9");
            List<double> sepMarksSubject = new();
            foreach (var m in septemberMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    sepMarksSubject.Add(mark);
                }
            }
            if (sepMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Сентябрь", (Math.Round(sepMarksSubject.Sum() / sepMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Сентябрь", "0");
            }


            var octoberMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "10");
            List<double> octMarksSubject = new();
            foreach (var m in octoberMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    octMarksSubject.Add(mark);
                }
            }
            if (octMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Октябрь", (Math.Round(octMarksSubject.Sum() / octMarksSubject.Count, 2)).ToString().ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Октябрь", "0");
            }


            var novemberMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "11");
            List<double> novMarksSubject = new();
            foreach (var m in novemberMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    novMarksSubject.Add(mark);
                }
            }
            if (novMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Ноябрь", (Math.Round(novMarksSubject.Sum() / novMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Ноябрь", "0");
            }


            var decemberMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "12");
            List<double> decMarksSubject = new();
            foreach (var m in decemberMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    decMarksSubject.Add(mark);
                }
            }
            if (decMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Декабрь", (Math.Round(decMarksSubject.Sum() / decMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Декабрь", "0");
            }

            var januaryMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "1");
            List<double> janMarksSubject = new();
            foreach (var m in januaryMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    janMarksSubject.Add(mark);
                }
            }
            if (janMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Январь", (Math.Round(janMarksSubject.Sum() / janMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Январь", "0");
            }

            var februaryMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "2");
            List<double> febMarksSubject = new();
            foreach (var m in februaryMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    febMarksSubject.Add(mark);
                }
            }
            if (febMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Февраль", (Math.Round(febMarksSubject.Sum() / febMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Февраль", "0");
            }

            var marchMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "3");
            List<double> marMarksSubject = new();
            foreach (var m in marchMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    marMarksSubject.Add(mark);
                }
            }
            if (marMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Март", (Math.Round(marMarksSubject.Sum() / marMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Март", "0");
            }

            var aprilMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "4");
            List<double> aprMarksSubject = new();
            foreach (var m in aprilMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    aprMarksSubject.Add(mark);
                }
            }
            if (aprMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Апрель", (Math.Round(aprMarksSubject.Sum() / aprMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Апрель", "0");
            }

            var mayMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "5");
            List<double> mMarksSubject = new();
            foreach (var m in mayMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    mMarksSubject.Add(mark);
                }
            }
            if (mMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Май", (Math.Round(mMarksSubject.Sum() / mMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Май", "0");
            }

            var juneMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "6");
            List<double> junMarksSubject = new();
            foreach (var m in juneMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    junMarksSubject.Add(mark);
                }
            }
            if (junMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Июнь", (Math.Round(junMarksSubject.Sum() / junMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Июнь", "0");
            }

            var julyMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "7");
            List<double> julMarksSubject = new();
            foreach (var m in julyMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    julMarksSubject.Add(mark);
                }
            }
            if (julMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Июль", (Math.Round(julMarksSubject.Sum() / julMarksSubject.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Июль", "0");
            }

            var augustMarksSubject = subjectMarks.Where(m => m.Date.Month.ToString() == "8");
            List<double> augMarksSubject = new();
            foreach (var m in augustMarksSubject)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    augMarksSubject.Add(mark);
                }
            }
            if (augMarksSubject.Count != 0)
            {
                raitingTimeSubject.Add("Август", (Math.Round(augMarksSubject.Sum() / augMarksSubject.Count)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTimeSubject.Add("Август", "0");
            }
            //конец блока

            //Общая среднемесячная успеваемость
            Dictionary<string, string> raitingTime = new();
            var septemberMarks = marks.Where(m => m.Date.Month.ToString() == "9");
            List<double> sepMarks = new();
            foreach (var m in septemberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    sepMarks.Add(mark);
                }
            }
            if (sepMarks.Count != 0)
            {
                raitingTime.Add("Сентябрь", (Math.Round(sepMarks.Sum() / sepMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Сентябрь", "0");
            }

            var octoberMarks = marks.Where(m => m.Date.Month.ToString() == "10");
            List<double> octMarks = new();
            foreach (var m in octoberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    octMarks.Add(mark);
                }
            }
            if (octMarks.Count != 0)
            {
                raitingTime.Add("Октябрь", (Math.Round(octMarks.Sum() / octMarks.Count, 2)).ToString().ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Октябрь", "0");
            }


            var novemberMarks = marks.Where(m => m.Date.Month.ToString() == "11");
            List<double> novMarks = new();
            foreach (var m in novemberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    novMarks.Add(mark);
                }
            }
            if (novMarks.Count != 0)
            {
                raitingTime.Add("Ноябрь", (Math.Round(novMarks.Sum() / novMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Ноябрь", "0");
            }


            var decemberMarks = marks.Where(m => m.Date.Month.ToString() == "12");
            List<double> decMarks = new();
            foreach (var m in decemberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    decMarks.Add(mark);
                }
            }
            if (decMarks.Count != 0)
            {
                raitingTime.Add("Декабрь", (Math.Round(decMarks.Sum() / decMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Декабрь", "0");
            }

            var januaryMarks = marks.Where(m => m.Date.Month.ToString() == "1");
            List<double> janMarks = new();
            foreach (var m in januaryMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    janMarks.Add(mark);
                }
            }
            if (janMarks.Count != 0)
            {
                raitingTime.Add("Январь", (Math.Round(janMarks.Sum() / janMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Январь", "0");
            }

            var februaryMarks = marks.Where(m => m.Date.Month.ToString() == "2");
            List<double> febMarks = new();
            foreach (var m in februaryMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    febMarks.Add(mark);
                }
            }
            if (febMarks.Count != 0)
            {
                raitingTime.Add("Февраль", (Math.Round(febMarks.Sum() / febMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Февраль", "0");
            }

            var marchMarks = marks.Where(m => m.Date.Month.ToString() == "3");
            List<double> marMarks = new();
            foreach (var m in marchMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    marMarks.Add(mark);
                }
            }
            if (marMarks.Count != 0)
            {
                raitingTime.Add("Март", (Math.Round(marMarks.Sum() / marMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Март", "0");
            }

            var aprilMarks = marks.Where(m => m.Date.Month.ToString() == "4");
            List<double> aprMarks = new();
            foreach (var m in aprilMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    aprMarks.Add(mark);
                }
            }
            if (aprMarks.Count != 0)
            {
                raitingTime.Add("Апрель", (Math.Round(aprMarks.Sum() / aprMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Апрель", "0");
            }

            var mayMarks = marks.Where(m => m.Date.Month.ToString() == "5");
            List<double> mMarks = new();
            foreach (var m in mayMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    mMarks.Add(mark);
                }
            }
            if (mMarks.Count != 0)
            {
                raitingTime.Add("Май", (Math.Round(mMarks.Sum() / mMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Май", "0");
            }

            var juneMarks = marks.Where(m => m.Date.Month.ToString() == "6");
            List<double> junMarks = new();
            foreach (var m in juneMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    junMarks.Add(mark);
                }
            }
            if (junMarks.Count != 0)
            {
                raitingTime.Add("Июнь", (Math.Round(junMarks.Sum() / junMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Июнь", "0");
            }

            var julyMarks = marks.Where(m => m.Date.Month.ToString() == "7");
            List<double> julMarks = new();
            foreach (var m in julyMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    julMarks.Add(mark);
                }
            }
            if (julMarks.Count != 0)
            {
                raitingTime.Add("Июль", (Math.Round(julMarks.Sum() / julMarks.Count, 2)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Июль", "0");
            }

            var augustMarks = marks.Where(m => m.Date.Month.ToString() == "8");
            List<double> augMarks = new();
            foreach (var m in augustMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    augMarks.Add(mark);
                }
            }
            if (augMarks.Count != 0)
            {
                raitingTime.Add("Август", (Math.Round(augMarks.Sum() / augMarks.Count)).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Август", "0");
            }

            //Диаграмма предметов, роза ветров
            var journals = student.Group.Journals;
            List<object> radar = new();
            foreach (var journal in journals)
            {
                List<double> val = new();
                var studentSubjectMarks = marks.Where(m => m.SubjectID == journal.SubjectID);
                foreach (var mark in studentSubjectMarks)
                {
                    if (double.TryParse(mark.Value, out var m))
                    {
                        val.Add(m);
                    }
                }

                if (val.Count != 0)
                {
                    double valRaiting = Math.Round(val.Sum() / val.Count, 2);
                    radar.Add(new { Subject = journal.Subject.ShortName.ToString(), Value = valRaiting.ToString().Replace(",", ".") });
                }
            }

            //Отрицательные результаты
            var negativeMarks = marks.Where(m => m.Value == "1" || m.Value == "2" || m.Value == "3");

            //Итоговые результаты обучения
            var statementMarks = _context.StatementMarks.Where(m => m.StudentID == id);
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
            var typeEKZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Экзамен");
            var typeDZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дифференцированный зачёт");
            var typeZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Зачёт");
            var typeF = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая оценка");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");

            List<MarkSubjectFinal> markSubjectFinals = new();
            var studentMarks = _context.Marks.Where(m => m.StudentID == id);
            foreach (var journal in journals)
            {
                var mrks = studentMarks.Where(m => m.SubjectID == journal.SubjectID && (m.TypeOfExerciseID == typeKM.TypeOfExerciseID || m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID || m.TypeOfExerciseID == typePZ.TypeOfExerciseID || m.TypeOfExerciseID == typeSZ.TypeOfExerciseID || m.TypeOfExerciseID == typeLZ.TypeOfExerciseID || m.TypeOfExerciseID == typeL.TypeOfExerciseID));
                List<double> simplemrks = new();
                foreach (var m in mrks)
                {
                    if (double.TryParse(m.Value, out var vm))
                    {
                        simplemrks.Add(vm);
                    }
                }

                var controlMarks = studentMarks.Where(m => m.SubjectID == journal.SubjectID && (m.TypeOfExerciseID == typeEKZ.TypeOfExerciseID || m.TypeOfExerciseID == typeDZ.TypeOfExerciseID || m.TypeOfExerciseID == typeZ.TypeOfExerciseID));
                List<Mark> controlmrks = new();
                foreach (var m in controlMarks)
                {
                    controlmrks.Add(m);
                }

                var fMarks = studentMarks.Where(m => m.SubjectID == journal.SubjectID && m.TypeOfExerciseID == typeF.TypeOfExerciseID);
                List<Mark> fmrks = new();
                foreach (var m in fMarks)
                {
                    fmrks.Add(m);
                }

                var kMarks = studentMarks.Where(m => m.SubjectID == journal.SubjectID && (m.TypeOfExerciseID == typeKP.TypeOfExerciseID || m.TypeOfExerciseID == typeKR.TypeOfExerciseID));
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

            ViewBag.AttendancePercent = attendancePercent;
            ViewBag.Attendance = attendanceNumber;
            ViewBag.SubjectFinals = markSubjectFinals;
            ViewBag.FinalMarks = finalMarks.OrderBy(m => m.Mark.Date);
            ViewBag.NegativeMarks = negativeMarks;
            ViewBag.Test = radar;
            ViewBag.Raiting = Math.Round(raiting, 2);
            ViewBag.MarksNumber = marksNumber;
            ViewBag.MarksPercent = marksPercent;
            ViewBag.Year = yearsStudy;
            ViewBag.SubjectRaiting = raitingTimeSubject;
            ViewBag.Subject = subject;
            ViewBag.TimeRaiting = raitingTime;

            return View(student);
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
