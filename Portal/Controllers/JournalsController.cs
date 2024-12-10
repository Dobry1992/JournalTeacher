using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Models.Model;
using Portal.ViewModel;

namespace Portal
{
    public class JournalsController : Controller
    {
        private readonly AcademyContext _context;

        public JournalsController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var journals = _context.Journals
                .Include(j => j.Group)
                .Include(j => j.Subject);
            return View(await journals.ToListAsync());
        }

        public IActionResult Final(int GroupID, int SubjectID)
        {
            var students = _context.Students.Where(s => s.GroupID == GroupID).AsNoTracking();
            List<ResultStudent> resultsStudent = new();
            foreach (var student in students)
            {
                ResultStudent resultStudent = new();
                List<double> marks = new();
                var simpleMarks = _context.Marks.Where(m => m.FlagF == 0 && m.GroupID == GroupID && m.SubjectID == SubjectID && m.StudentID == student.StudentID).AsNoTracking();
                foreach (var mark in simpleMarks)
                {
                    if (double.TryParse(mark.Value, out double m))
                    {
                        marks.Add(m);
                    }
                }
                resultStudent.Student = student;
                resultStudent.Value = Math.Round(marks.Sum() / marks.Count, 2);
                resultsStudent.Add(resultStudent);
            }

            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;
            return View(resultsStudent.OrderBy(s => s.Student.LastName));
        }

        public async Task<IActionResult> Statement(int GroupID)
        {
            var typeDR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дипломная работа");
            var typeGE = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Государственный экзамен");
            var typeS = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Стажировка");
            var typePP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Производственная практика");
            var typeUP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Учебная практика");
            var typeDP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дипломный проект");
            var typeMR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Магистерская работа");

            var group = await _context.Groups.FindAsync(GroupID);
            var inst = await _context.Institutes.FindAsync(group.InstituteID);

            var lessons = _context.StatementLessons.Where(l => l.GroupID == GroupID && (l.TypeOfExerciseID == typeDR.TypeOfExerciseID || l.TypeOfExerciseID == typeGE.TypeOfExerciseID
                || l.TypeOfExerciseID == typeS.TypeOfExerciseID || l.TypeOfExerciseID == typePP.TypeOfExerciseID || l.TypeOfExerciseID == typeUP.TypeOfExerciseID
                || l.TypeOfExerciseID == typeDP.TypeOfExerciseID || l.TypeOfExerciseID == typeMR.TypeOfExerciseID))
                .OrderBy(l => l.Date);

            var students = _context.Students
               .Where(s => s.GroupID == GroupID && s.Status == true)
               .OrderBy(s => s.LastName)
               .Include(s => s.Marks)
                   .ThenInclude(m => m.Theme)
               .Include(s => s.Group)
                   .ThenInclude(g => g.Lessons);

            var marks = _context.StatementMarks
                .Where(m => m.GroupID == GroupID && (m.TypeOfExerciseID == typeDR.TypeOfExerciseID || m.TypeOfExerciseID == typeGE.TypeOfExerciseID
                    || m.TypeOfExerciseID == typeS.TypeOfExerciseID || m.TypeOfExerciseID == typePP.TypeOfExerciseID || m.TypeOfExerciseID == typeUP.TypeOfExerciseID
                    || m.TypeOfExerciseID == typeDP.TypeOfExerciseID || m.TypeOfExerciseID == typeMR.TypeOfExerciseID))
                .OrderBy(m => m.Date);

            ViewBag.Group = group;
            ViewBag.Marks = marks;
            ViewBag.Students = students;
            ViewBag.GroupID = GroupID;
            ViewBag.typeDR = typeDR.TypeOfExerciseID;
            ViewBag.typeGE = typeGE.TypeOfExerciseID;
            ViewBag.typeS = typeS.TypeOfExerciseID;
            ViewBag.typePP = typePP.TypeOfExerciseID;
            ViewBag.typeUP = typeUP.TypeOfExerciseID;
            ViewBag.typeDP = typeDP.TypeOfExerciseID;
            ViewBag.typeMR = typeMR.TypeOfExerciseID;
            ViewBag.Institute = inst;

            return View(await lessons.ToListAsync());
        }

        public async Task<IActionResult> ElectiveJournal(int electiveID)
        {
            var elective = await _context.Electives.FindAsync(electiveID);
            var links = await _context.El_Stud_Links.Where(l => l.ElectiveID == electiveID).ToListAsync();
            List<Student> students = new();
            foreach (var link in links) 
            {
                Student student = _context.Students.Find(link.StudentID);
                students.Add(student);
            }

            var lessons = await _context.ElectiveLessons
                .Where(l => l.Theme.ElectiveID == electiveID)
                .ToListAsync();

            var marks = await _context.ElectiveMarks
                .Where(m => m.ElectiveLesson.Theme.ElectiveID == electiveID)
                .ToListAsync();

            ViewBag.Elective = elective;
            ViewBag.Students = students;
            ViewBag.Marks = marks;

            return View(lessons);
        }

        public async Task<IActionResult> Journal(int GroupID, int SubjectID)
        {
            var group = await _context.Groups.FindAsync(GroupID);
            var subject = await _context.Subjects.FindAsync(SubjectID);
            var department = await _context.Departments.FindAsync(subject.DepartmentID);
            var journals = await _context.Journals.Where(j => j.GroupID == GroupID && j.SubjectID == SubjectID).AsNoTracking().ToListAsync();

            if (!journals.Any())
            {
                ViewBag.GroupID = GroupID;
                ViewBag.SubjectID = SubjectID;
                ViewBag.CountFlag = journals.Count();
                return View();
            }

            var lessons = await _context.Lessons
                .Where(l => l.Theme.SubjectID == SubjectID && l.GroupID == GroupID)
                .OrderBy(l => l.Date)
                .Include(l => l.TypeOfExercise)
                .Include(l => l.Theme)
                .AsNoTracking()
                .ToListAsync();

            var students = await _context.Students
                .Where(s => s.GroupID == GroupID && s.Status == true)
                .OrderBy(s => s.LastName)
                .AsNoTracking()
                .ToListAsync();

            var marks = await _context.Marks
                .Where(m => m.SubjectID == SubjectID && m.GroupID == GroupID)
                .OrderBy(m => m.Date)
                .AsNoTracking()
                .ToListAsync();

            var typeOfExerciseEKZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Экзамен");
            var typeOfExersiceDZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дифференцированный зачёт");
            var typeOfExersiceZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Зачёт");
            var typeOfExersiceIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая оценка");
            var typeOfExersiceKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
            var typeOfExersiceKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");

            var statementLessons = lessons
                .Where(l => l.TypeOfExerciseID == typeOfExerciseEKZ.TypeOfExerciseID || l.TypeOfExerciseID == typeOfExersiceDZ.TypeOfExerciseID || l.TypeOfExerciseID == typeOfExersiceZ.TypeOfExerciseID)
                .OrderBy(l => l.Date);

            foreach (var student in students)
            {
                var studMarksIO = marks.Where(m => m.StudentID == student.StudentID && m.TypeOfExerciseID == typeOfExersiceIO.TypeOfExerciseID);
                if (studMarksIO != null)
                {
                    foreach (var studMarkIO in studMarksIO)
                    {
                        var studMarksF = marks.Where(m => m.FlagF == studMarkIO.FlagF && m.StudentID == student.StudentID && m.TypeOfExerciseID != typeOfExersiceKP.TypeOfExerciseID && m.TypeOfExerciseID != typeOfExersiceKR.TypeOfExerciseID && m.TypeOfExerciseID != typeOfExersiceIO.TypeOfExerciseID && m.TypeOfExerciseID != typeOfExerciseEKZ.TypeOfExerciseID && m.TypeOfExerciseID != typeOfExersiceDZ.TypeOfExerciseID && m.TypeOfExerciseID != typeOfExersiceZ.TypeOfExerciseID);
                        var studMarkEKZ = marks.FirstOrDefault(m => m.FlagF == studMarkIO.FlagF && m.StudentID == student.StudentID && (m.TypeOfExerciseID == typeOfExerciseEKZ.TypeOfExerciseID || m.TypeOfExerciseID == typeOfExersiceDZ.TypeOfExerciseID || m.TypeOfExerciseID == typeOfExersiceZ.TypeOfExerciseID));
                        //Экзамен (Дифференцированный зачёт)
                        if (studMarkEKZ.TypeOfExerciseID == typeOfExerciseEKZ.TypeOfExerciseID || studMarkEKZ.TypeOfExerciseID == typeOfExersiceDZ.TypeOfExerciseID)
                        {
                            List<double> marksDouble = new();
                            foreach (var mark in studMarksF)
                            {
                                if (double.TryParse(mark.Value, out double markF))
                                    marksDouble.Add(markF);
                            }

                            if (marksDouble.Count > 0)
                            {
                                if (int.TryParse(studMarkEKZ.Value, out int EKZ))
                                {
                                    if (EKZ < 4)
                                    {
                                        studMarkIO.Value = "Незачёт";
                                        _context.Marks.Update(studMarkIO);
                                    }
                                    else if ((marksDouble.Sum() / marksDouble.Count) < 4)
                                    {
                                        studMarkEKZ.Value = "Н/Д";
                                        _context.Marks.Update(studMarkEKZ);
                                        studMarkIO.Value = "Н/Д";
                                        _context.Marks.Update(studMarkIO);
                                    }
                                    else
                                    {
                                        studMarkIO.Value = (Math.Round(marksDouble.Sum() * 0.6 / marksDouble.Count + EKZ * 0.4)).ToString();
                                        _context.Marks.Update(studMarkIO);
                                    }
                                }
                                else if ((marksDouble.Sum() / marksDouble.Count) < 4)
                                {
                                    studMarkEKZ.Value = "Н/Д";
                                    _context.Marks.Update(studMarkEKZ);
                                    studMarkIO.Value = "Н/Д";
                                    _context.Marks.Update(studMarkIO);
                                }
                                else
                                {
                                    studMarkEKZ.Value = "";
                                    _context.Marks.Update(studMarkEKZ);
                                    studMarkIO.Value = "";
                                    _context.Marks.Update(studMarkIO);
                                }
                            }
                            else
                            {
                                studMarkEKZ.Value = "Н/Д";
                                _context.Marks.Update(studMarkEKZ);
                                studMarkIO.Value = "Н/Д";
                                _context.Marks.Update(studMarkIO);
                            }
                        }

                        //Зачёт
                        if (studMarkEKZ.TypeOfExerciseID == typeOfExersiceZ.TypeOfExerciseID)
                        {
                            List<double> marksDouble = new();
                            foreach (var mark in studMarksF)
                            {
                                if (double.TryParse(mark.Value, out var markF))
                                    marksDouble.Add(markF);
                            }

                            if (marksDouble.Count > 0)
                            {
                                if (int.TryParse(studMarkEKZ.Value, out var Z))
                                {
                                    double finishMark = marksDouble.Sum() * 0.6 / marksDouble.Count + Z * 0.4;
                                    if (Z < 4)
                                    {
                                        studMarkIO.Value = "Незачтено";
                                        studMarkIO.Comment = finishMark.ToString();
                                        _context.Marks.Update(studMarkIO);
                                    }
                                    else if ((marksDouble.Sum() / marksDouble.Count) < 4)
                                    {
                                        studMarkEKZ.Value = "Н/Д";
                                        _context.Marks.Update(studMarkEKZ);
                                        studMarkIO.Value = "Н/Д";
                                        _context.Marks.Update(studMarkIO);
                                    }
                                    else
                                    {
                                        studMarkIO.Value = "Зачтено";
                                        studMarkIO.Comment = finishMark.ToString();
                                        _context.Marks.Update(studMarkIO);
                                    }
                                }
                                else if ((marksDouble.Sum() / marksDouble.Count) < 4)
                                {
                                    studMarkEKZ.Value = "Н/Д";
                                    _context.Marks.Update(studMarkEKZ);
                                    studMarkIO.Value = "Н/Д";
                                    _context.Marks.Update(studMarkIO);
                                }
                                else if (studMarkEKZ.Value == "З")
                                {
                                    studMarkIO.Value = "Зачтено";
                                    _context.Marks.Update(studMarkIO);
                                }
                                else if (studMarkEKZ.Value == "НЗ")
                                {
                                    studMarkIO.Value = "Незачтено";
                                    _context.Marks.Update(studMarkIO);
                                }
                                else
                                {
                                    studMarkEKZ.Value = "";
                                    _context.Marks.Update(studMarkEKZ);
                                    studMarkIO.Value = "";
                                    _context.Marks.Update(studMarkIO);
                                }
                            }
                            else
                            {
                                studMarkEKZ.Value = "Н/Д";
                                _context.Marks.Update(studMarkEKZ);
                                studMarkIO.Value = "Н/Д";
                                _context.Marks.Update(studMarkIO);
                            }
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();

            List<JournalMarks> journalMarks = new();
            foreach (var mark in marks)
            {
                if (mark.TypeOfExerciseID == typeOfExerciseEKZ.TypeOfExerciseID || mark.TypeOfExerciseID == typeOfExersiceZ.TypeOfExerciseID || mark.TypeOfExerciseID == typeOfExersiceDZ.TypeOfExerciseID)
                {
                    JournalMarks jm = new();
                    jm.Mark = mark;
                    jm.Property = "tableMarkEKZ";
                    jm.Action = "Edit";
                    jm.Controller = "Marks";
                    journalMarks.Add(jm);
                }
                else if (mark.TypeOfExerciseID == typeOfExersiceIO.TypeOfExerciseID)
                {
                    JournalMarks jm = new();
                    jm.Mark = mark;
                    jm.Property = "tableMarkIO";
                    jm.Action = "Journal";
                    jm.Controller = "Journals";
                    journalMarks.Add(jm);
                }
                else if (mark.TypeOfExerciseID == typeOfExersiceKP.TypeOfExerciseID || mark.TypeOfExerciseID == typeOfExersiceKR.TypeOfExerciseID)
                {
                    JournalMarks jm = new();
                    jm.Mark = mark;
                    jm.Property = "tableMarkK";
                    jm.Action = "Edit";
                    jm.Controller = "Marks";
                    journalMarks.Add(jm);
                }
                else
                {
                    if (mark.FlagF == 0)
                    {
                        JournalMarks jm = new();
                        jm.Mark = mark;
                        jm.Property = "tableMark";
                        jm.Action = "Edit";
                        jm.Controller = "Marks";
                        journalMarks.Add(jm);
                    }
                    else
                    {
                        JournalMarks jm = new();
                        jm.Mark = mark;
                        jm.Property = "tableMarkSet";
                        jm.Action = "Edit";
                        jm.Controller = "Marks";
                        journalMarks.Add(jm);
                    }
                }
            }

            List<JournalLessons> journalLessons = new();
            foreach (var lesson in lessons)
            {
                if (lesson.TypeOfExerciseID == typeOfExerciseEKZ.TypeOfExerciseID || lesson.TypeOfExerciseID == typeOfExersiceZ.TypeOfExerciseID || lesson.TypeOfExerciseID == typeOfExersiceDZ.TypeOfExerciseID)
                {
                    JournalLessons jl = new();
                    jl.Action = "DeleteF";
                    jl.Lesson = lesson;
                    jl.Controller = "Lessons";
                    journalLessons.Add(jl);
                }
                else if (lesson.TypeOfExerciseID == typeOfExersiceIO.TypeOfExerciseID)
                {
                    JournalLessons jl = new();
                    jl.Action = "Journal";
                    jl.Lesson = lesson;
                    jl.Controller = "Journals";
                    journalLessons.Add(jl);
                }
                else
                {
                    JournalLessons jl = new();
                    jl.Action = "Delete";
                    jl.Lesson = lesson;
                    jl.Controller = "Lessons";
                    journalLessons.Add(jl);
                }
            }

            ViewBag.Marks = journalMarks.OrderBy(m => m.Mark.Date);
            ViewBag.Students = students;
            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;
            ViewBag.Department = department;
            ViewBag.Subject = subject;
            ViewBag.Group = group;
            ViewBag.StatementLessons = statementLessons;

            return View(journalLessons.OrderBy(l => l.Lesson.Date));
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create(int GroupID, int SubjectID)
        {
            ViewData["GroupID"] = new SelectList(_context.Groups.Where(g => g.GroupID == GroupID), "GroupID", "Name");
            ViewData["SubjectID"] = new SelectList(_context.Subjects.Where(s => s.SubjectID == SubjectID), "SubjectID", "Name");
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int GroupID, int SubjectID, [Bind("JournalID,Comment,GroupID,SubjectID,Date")] Journal journal)
        {
            if (ModelState.IsValid)
            {
                journal.Date = DateTime.Now;
                _context.Add(journal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupID"] = new SelectList(_context.Groups.Where(g => g.GroupID == GroupID), "GroupID", "Name", journal.GroupID);
            ViewData["SubjectID"] = new SelectList(_context.Subjects.Where(s => s.SubjectID == SubjectID), "SubjectID", "Name", journal.SubjectID);
            return View(journal);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult CreateJournal()
        {
            ViewData["GroupID"] = new SelectList(_context.Groups, "GroupID", "Name");
            ViewData["SubjectID"] = new SelectList(_context.Subjects, "SubjectID", "Name");
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJournal([Bind("JournalID,Comment,GroupID,SubjectID,Date")] Journal journal)
        {
            if (ModelState.IsValid)
            {
                journal.Date = DateTime.Now;
                _context.Add(journal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupID"] = new SelectList(_context.Groups, "GroupID", "Name", journal.GroupID);
            ViewData["SubjectID"] = new SelectList(_context.Subjects, "SubjectID", "Name", journal.SubjectID);
            return View(journal);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var journal = await _context.Journals
                .Include(j => j.Group)
                .Include(j => j.Subject)
                .FirstOrDefaultAsync(m => m.JournalID == id);
            if (journal == null)
            {
                return NotFound();
            }

            return View(journal);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var journal = await _context.Journals.FindAsync(id);
            _context.Journals.Remove(journal);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool JournalExists(int id)
        {
            return _context.Journals.Any(e => e.JournalID == id);
        }
    }
}