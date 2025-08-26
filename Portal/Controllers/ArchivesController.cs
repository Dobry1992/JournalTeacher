using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class ArchivesController : Controller
    {
        private readonly AcademyContext _context;

        public ArchivesController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> IndexJournals(DateTime date_1, DateTime date_2)
        {

            var journals = _context.JournalArhives
                .Include(g => g.GroupArhive)
                .Include(s => s.Subject)
                .Where(j => j.Date.Year >= date_1.Year && j.Date.Year <= date_2.Year);
            return View(await journals.ToListAsync());
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> ArchiveJournal(int GroupID, int SubjectID)
        {
            var group = await _context.GroupArhives.FindAsync(GroupID);
            var subject = await _context.Subjects.FindAsync(SubjectID);
            var department = await _context.Departments.FindAsync(subject.DepartmentID);

            var lessons = _context.LessonArhives
                .Where(l => l.Theme.SubjectID == SubjectID && l.GroupArhiveID == GroupID)
                .OrderBy(l => l.Date)
                .Include(l => l.TypeOfExercise)
                .Include(l => l.Theme)
                    .ThenInclude(t => t.Subject)
                .Include(l => l.GroupArhive)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(s => s.Marks)
                .AsNoTracking();

            var students = _context.StudentArhives
                .Where(s => s.GroupArhiveID == GroupID)
                .OrderBy(s => s.LastName)
                .Include(s => s.Marks)
                    .ThenInclude(m => m.Theme)
                .Include(s => s.GroupArhive)
                    .ThenInclude(g => g.Lessons)
                .AsNoTracking();

            var marks = _context.MarkArhives
                .Where(m => m.SubjectID == SubjectID && m.GroupID == GroupID)
                .OrderBy(m => m.Date)
                .AsNoTracking();

            var typeOfExerciseEKZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Экзамен");
            var typeOfExersiceDZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дифференцированный зачёт");
            var typeOfExersiceZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Зачёт");
            var typeOfExersiceIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая отметка");
            var typeOfExersiceKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
            var typeOfExersiceKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");

            var statementLessons = lessons.Where(l => l.TypeOfExerciseID == typeOfExerciseEKZ.TypeOfExerciseID || l.TypeOfExerciseID == typeOfExersiceDZ.TypeOfExerciseID || l.TypeOfExerciseID == typeOfExersiceZ.TypeOfExerciseID).AsNoTracking();

            List<JournalMarksArchive> journalMarks = new();
            foreach (var mark in marks)
            {
                if (mark.TypeOfExerciseID == typeOfExerciseEKZ.TypeOfExerciseID || mark.TypeOfExerciseID == typeOfExersiceZ.TypeOfExerciseID || mark.TypeOfExerciseID == typeOfExersiceDZ.TypeOfExerciseID)
                {
                    JournalMarksArchive jm = new();
                    jm.Mark = mark;
                    jm.Property = "tableMarkEKZ";
                    journalMarks.Add(jm);
                }
                else if (mark.TypeOfExerciseID == typeOfExersiceIO.TypeOfExerciseID)
                {
                    JournalMarksArchive jm = new();
                    jm.Mark = mark;
                    jm.Property = "tableMarkIO";
                    journalMarks.Add(jm);
                }
                else if (mark.TypeOfExerciseID == typeOfExersiceKP.TypeOfExerciseID || mark.TypeOfExerciseID == typeOfExersiceKR.TypeOfExerciseID)
                {
                    JournalMarksArchive jm = new();
                    jm.Mark = mark;
                    jm.Property = "tableMarkK";
                    journalMarks.Add(jm);
                }
                else
                {
                    if (mark.FlagF != 0)
                    {
                        JournalMarksArchive jm = new();
                        jm.Mark = mark;
                        jm.Property = "tableMark";
                        journalMarks.Add(jm);
                    }
                    else
                    {
                        JournalMarksArchive jm = new();
                        jm.Mark = mark;
                        jm.Property = "tableMarkSet";
                        journalMarks.Add(jm);
                    }
                }
            }

            ViewBag.Marks = journalMarks;
            ViewBag.Students = students;
            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;
            ViewBag.Department = department;
            ViewBag.Subject = subject;
            ViewBag.Group = group;
            ViewBag.StatementLessons = statementLessons;

            return View(lessons);
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> IndexGroups(string searchString)
        {
            var groups = from g in _context.GroupArhives
                         select g;

            var intitutes = from i in _context.Institutes
                            orderby i.Name
                            select i;

            if (!String.IsNullOrEmpty(searchString))
            {
                groups = groups.Where(g => g.Name == searchString || g.DateExit.Year.ToString() == searchString || g.DateEnter.Year.ToString() == searchString);
            }

            ViewBag.Institutes = intitutes;

            return View(await groups
                .Include(g => g.Speciality)
                    .ThenInclude(s => s.Institute)
                .AsNoTracking()
                .OrderBy(g => g.DateEnter)
                .OrderBy(i => i.InstituteID)
                .ToListAsync());
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> ArchiveGroup(int id)
        {
            var group = await _context.GroupArhives.FindAsync(id);
            var institute = await _context.Institutes.FindAsync(group.InstituteID);
            var marks = _context.MarkArhives.Where(m => m.GroupID == id);
            var students = _context.StudentArhives
                .Include(m => m.Marks)
                .Where(s => s.GroupArhiveID == id);

            List<double> doubleMarks = new();
            foreach (var mark in marks)
            {
                if (double.TryParse(mark.Value, out double number))
                {
                    doubleMarks.Add(number);
                }
            }
            double raiting = doubleMarks.Sum() / doubleMarks.Count;

            List<GroupArchiveRaiting> groupArchiveRaitings = new();
            foreach (var student in students)
            {
                List<double> numMarks = new();
                foreach (var mark in student.Marks)
                {
                    if (double.TryParse(mark.Value, out double number))
                    {
                        numMarks.Add(number);
                    }
                }
                GroupArchiveRaiting gar = new();
                gar.Student = student;
                gar.Raiting = Math.Round(numMarks.Sum() / numMarks.Count, 3, MidpointRounding.AwayFromZero);
                gar.Count = numMarks.Count;
                groupArchiveRaitings.Add(gar);
            }
            var groupArchiveRaitingsOrdered = groupArchiveRaitings.OrderByDescending(g => g.Raiting);

            List<GroupArchiveStat> groupArchiveStats = new();
            for (int i = 1; i <= 10; i++)
            {
                int count = 0;
                List<double> marksDouble = new();
                foreach (var mark in marks)
                {
                    if (double.TryParse(mark.Value, out double number))
                    {
                        marksDouble.Add(number);
                        if (number == i)
                        {
                            count++;
                        }
                    }
                }
                double n = marksDouble.Count;
                double percent = (double)count / n * 100;
                GroupArchiveStat gas = new();
                gas.Value = i;
                gas.Count = count;
                gas.Percent = Math.Round(percent, 3, MidpointRounding.AwayFromZero);
                groupArchiveStats.Add(gas);
            }
            var groupArchiveStatsOrderd = groupArchiveStats.OrderBy(m => m.Value);

            ViewBag.Institute = institute;
            ViewBag.Raiting = Math.Round(raiting, 3, MidpointRounding.AwayFromZero);
            ViewBag.Students = students.Count();
            ViewBag.BestStudent = groupArchiveRaitingsOrdered.ToList()[0];
            ViewBag.WorseStudent = groupArchiveRaitingsOrdered.ToList()[groupArchiveRaitingsOrdered.Count() - 1];
            ViewBag.MarksRaiting = groupArchiveStatsOrderd.ToList();
            ViewBag.GroupRaiting = groupArchiveRaitingsOrdered.ToList();

            return View(group);
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> IndexStudents(string searchString)
        {
            var students = from s in _context.StudentArhives
                           select s;

            students = students.Where(s => s.LastName == searchString);

            return View(await students.AsNoTracking()
                .Include(s => s.GroupArhive)
                    .ThenInclude(g => g.Speciality)
                        .ThenInclude(s => s.Institute)
                .OrderBy(s => s.LastName)
                .ToListAsync());
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> ArchiveStudent(int id)
        {
            var student = await _context.StudentArhives.FindAsync(id);
            var group = await _context.GroupArhives.FindAsync(student.GroupArhiveID);
            var speciality = await _context.Specialities.FindAsync(group.SpecialityID);
            var institute = await _context.Institutes.FindAsync(student.InstituteID);
            var marks = _context.MarkArhives.Where(m => m.StudentArhiveID == id);

            List<double> marksStat = new();
            foreach (var mark in marks)
            {
                if (double.TryParse(mark.Value, out double number))
                {
                    marksStat.Add(number);
                }
            }

            List<GroupArchiveStat> groupArchiveStats = new();
            for (int i = 1; i <= 10; i++)
            {
                int count = 0;
                List<double> marksDouble = new();
                foreach (var mark in marks)
                {
                    if (double.TryParse(mark.Value, out double number))
                    {
                        marksDouble.Add(number);
                        if (number == i)
                        {
                            count++;
                        }
                    }
                }
                double n = marksDouble.Count;
                double percent = (double)count / n * 100;
                GroupArchiveStat gas = new();
                gas.Value = i;
                gas.Count = count;
                gas.Percent = Math.Round(percent, 3, MidpointRounding.AwayFromZero);
                groupArchiveStats.Add(gas);
            }
            var groupArchiveStatsOrderd = groupArchiveStats.OrderBy(m => m.Value);

            //Результаты обучения по предметам
            var typeEKZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Экзамен");
            var typeDZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дифференцированный зачёт");
            var typeZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Зачёт");
            var typeF = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая отметка");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            var typeSZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Семинарское занятие");
            var typePZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Практическое занятие");
            var typeLZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лабораторное занятие");
            var typeL = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лекция");
            var journals = _context.JournalArhives
                .Include(j => j.Subject)
                .Where(j => j.GroupArhiveID == group.GroupArhiveID);

            List<ArchiveMarkStat> markSubjectFinals = new();
            var studentMarks = _context.MarkArhives.Where(m => m.StudentArhiveID == id);
            foreach (var journal in journals)
            {
                var mrks = studentMarks.Where(m => m.SubjectID == journal.SubjectID && (m.TypeOfExerciseID == typePZ.TypeOfExerciseID || m.TypeOfExerciseID == typeSZ.TypeOfExerciseID || m.TypeOfExerciseID == typeLZ.TypeOfExerciseID || m.TypeOfExerciseID == typeL.TypeOfExerciseID));
                List<double> simplemrks = new();
                foreach (var m in mrks)
                {
                    if (double.TryParse(m.Value, out var vm))
                    {
                        simplemrks.Add(vm);
                    }
                }

                var controlMarks = studentMarks.Where(m => m.SubjectID == journal.SubjectID && (m.TypeOfExerciseID == typeEKZ.TypeOfExerciseID || m.TypeOfExerciseID == typeDZ.TypeOfExerciseID || m.TypeOfExerciseID == typeZ.TypeOfExerciseID));
                List<MarkArhive> controlmrks = new();
                foreach (var m in controlMarks)
                {
                    controlmrks.Add(m);
                }

                var fMarks = studentMarks.Where(m => m.SubjectID == journal.SubjectID && m.TypeOfExerciseID == typeF.TypeOfExerciseID);
                List<MarkArhive> fmrks = new();
                foreach (var m in fMarks)
                {
                    fmrks.Add(m);
                }

                var kMarks = studentMarks.Where(m => m.SubjectID == journal.SubjectID && (m.TypeOfExerciseID == typeKP.TypeOfExerciseID || m.TypeOfExerciseID == typeKR.TypeOfExerciseID));
                List<MarkArhive> kmarks = new();
                foreach (var m in kMarks)
                {
                    kmarks.Add(m);
                }

                ArchiveMarkStat msf = new();
                msf.Subject = journal.Subject;
                msf.Value = Math.Round(simplemrks.Sum() / simplemrks.Count, 3, MidpointRounding.AwayFromZero);
                msf.ControlMarks = controlmrks;
                msf.FinalMarks = fmrks;
                msf.ValueK = kmarks;
                markSubjectFinals.Add(msf);
            }

            var statementMarks = _context.StatementMarkArhives.Where(m => m.StudentArhiveID == id);
            List<ArchiveFinalMark> finalMarks = new();
            foreach (var mark in statementMarks)
            {
                TypeOfExercise t = await _context.Types.FindAsync(mark.TypeOfExerciseID);
                ArchiveFinalMark m = new();
                m.Mark = mark;
                m.Type = t;
                finalMarks.Add(m);
            }

            ViewBag.Speciality = speciality;
            ViewBag.Group = group;
            ViewBag.Raiting = Math.Round(marksStat.Sum() / marksStat.Count, 3, MidpointRounding.AwayFromZero);
            ViewBag.Institute = institute;
            ViewBag.StudentRaiting = groupArchiveStatsOrderd.ToList();
            ViewBag.SubjectFinals = markSubjectFinals;
            ViewBag.FinalMarks = finalMarks.OrderBy(m => m.Mark.Date);

            return View(student);
        }
    }
}
