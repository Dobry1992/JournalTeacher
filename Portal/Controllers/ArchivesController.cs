using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

            if (!String.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.LastName == searchString);
            }

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

        // ============================================================
        // ОТПРАВКА В АРХИВ
        // ============================================================

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> GroupToArchive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.Groups
                .Include(g => g.Students)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GroupID == id);

            if (group == null)
            {
                return NotFound();
            }

            ViewBag.StudentsCount = group.Students.Count;
            ViewBag.LessonsCount = await _context.Lessons.CountAsync(l => l.GroupID == id);
            ViewBag.StatementLessonsCount = await _context.StatementLessons.CountAsync(l => l.GroupID == id);
            ViewBag.MarksCount = await _context.Marks.CountAsync(m => m.GroupID == id);
            ViewBag.JournalsCount = await _context.Journals.CountAsync(j => j.GroupID == id);
            ViewBag.HasSameArchive = await _context.GroupArhives.AnyAsync(g =>
                g.Name == group.Name && g.DateEnter == group.DateEnter && g.DateExit == group.DateExit);

            return View(group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("GroupToArchive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GroupToArchiveConfirmed(int id)
        {
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupID == id);

            if (group == null)
            {
                return NotFound();
            }

            string groupName = group.Name;
            string error = await RunInTransactionAsync(() => ArchiveGroupAsync(group));

            if (error != null)
            {
                TempData["ErrorMessage"] = error;
            }
            else
            {
                TempData["SuccessMessage"] = $"Группа \"{groupName}\" отправлена в архив";
            }

            return RedirectToAction("Index", "Groups");
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> StudentToArchive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Group)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
            {
                return NotFound();
            }

            ViewBag.MarksCount = await _context.Marks.CountAsync(m => m.StudentID == id);
            ViewBag.StatementMarksCount = await _context.StatementMarks.CountAsync(m => m.StudentID == id);
            ViewBag.HasSameArchive = await _context.GroupArhives.AnyAsync(g =>
                g.Name == student.Group.Name && g.DateEnter == student.Group.DateEnter && g.DateExit == student.Group.DateExit);

            return View(student);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("StudentToArchive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentToArchiveConfirmed(int id)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
            {
                return NotFound();
            }

            string studentName = $"{student.LastName} {student.Name} {student.Surname}";
            string error = await RunInTransactionAsync(() => ArchiveSingleStudentAsync(student));

            if (error != null)
            {
                TempData["ErrorMessage"] = error;
            }
            else
            {
                TempData["SuccessMessage"] = $"Курсант \"{studentName}\" отправлен в архив";
            }

            return RedirectToAction("Index", "Students");
        }

        // ============================================================
        // АРХИВИРОВАНИЕ: ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ============================================================

        // Архивирование состоит из нескольких сохранений, поэтому всё выполняется
        // в одной транзакции: при ошибке не должно остаться неполного архива
        private async Task<string> RunInTransactionAsync(Func<Task> operation)
        {
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    await operation();
                    await transaction.CommitAsync();
                });

                return null;
            }
            catch (RetryLimitExceededException)
            {
                return "Возникла ошибка обратитесь к администратору. Изменения не сохранены.";
            }
            catch (DbUpdateException)
            {
                return "Не удалось отправить в архив. Изменения не сохранены, обратитесь к администратору.";
            }
        }

        private async Task ArchiveGroupAsync(Group group)
        {
            GroupArhive groupArchive = await GetOrCreateGroupArchiveAsync(group);
            ArchiveMaps maps = await LoadArchiveMapsAsync(groupArchive.GroupArhiveID);

            // Занятия копируются все, чтобы архивный журнал соответствовал рабочему
            var groupLessons = await _context.Lessons
                .Where(l => l.GroupID == group.GroupID)
                .ToListAsync();

            await RegisterLessonArchivesAsync(groupArchive, maps,
                groupLessons.Select(lesson => (LessonKey: LessonKey(groupArchive.GroupArhiveID, lesson.ThemeID, lesson.TypeOfExerciseID, lesson.Date, lesson.SubjectID),
                                               Archive: BuildLessonArchive(groupArchive.GroupArhiveID, lesson))));

            var students = await _context.Students
                .Where(s => s.GroupID == group.GroupID)
                .ToListAsync();

            foreach (var student in students)
            {
                await ArchiveStudentMarksAsync(student, group, groupArchive, maps);
            }

            var journalSubjects = await _context.Journals
                .Where(j => j.GroupID == group.GroupID)
                .Select(j => j.SubjectID)
                .ToListAsync();

            await ArchiveJournalsAsync(group, groupArchive, maps, journalSubjects);

            var links = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == group.GroupID)
                .ToListAsync();
            _context.SubgroupLinks.RemoveRange(links);

            // Каскад удаляет курсантов группы, её занятия, ведомости и журналы
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
        }

        // Курсант архивируется в архивное «зеркало» своей группы, поэтому группа
        // может быть отправлена в архив целиком позже — записи дополнят друг друга
        private async Task ArchiveSingleStudentAsync(Student student)
        {
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupID == student.GroupID);

            if (group == null)
            {
                throw new InvalidOperationException($"Не найдена группа курсанта {student.StudentID}");
            }

            GroupArhive groupArchive = await GetOrCreateGroupArchiveAsync(group);
            ArchiveMaps maps = await LoadArchiveMapsAsync(groupArchive.GroupArhiveID);

            await ArchiveStudentMarksAsync(student, group, groupArchive, maps);

            var links = await _context.SubgroupLinks
                .Where(sl => sl.StudentID == student.StudentID && sl.GroupID == group.GroupID)
                .ToListAsync();
            _context.SubgroupLinks.RemoveRange(links);

            // Каскад удаляет отметки и итоговые отметки курсанта
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
        }

        private async Task<GroupArhive> GetOrCreateGroupArchiveAsync(Group group)
        {
            var groupArchive = await _context.GroupArhives.FirstOrDefaultAsync(g =>
                g.Name == group.Name &&
                g.DateEnter == group.DateEnter &&
                g.DateExit == group.DateExit &&
                g.InstituteID == group.InstituteID &&
                g.SpecialityID == group.SpecialityID);

            if (groupArchive != null)
            {
                return groupArchive;
            }

            groupArchive = new GroupArhive
            {
                Name = group.Name,
                DateEnter = group.DateEnter,
                DateExit = group.DateExit,
                InstituteID = group.InstituteID,
                SpecialityID = group.SpecialityID
            };

            _context.GroupArhives.Add(groupArchive);
            await _context.SaveChangesAsync();

            return groupArchive;
        }

        // Отметки и итоговые отметки одного курсанта. Занятия и ведомости создаются
        // одним сохранением и только если их ещё нет в архиве этой группы
        private async Task ArchiveStudentMarksAsync(Student student, Group group, GroupArhive groupArchive, ArchiveMaps maps)
        {
            StudentArhive studentArchive = new()
            {
                Name = student.Name,
                Surname = student.Surname,
                LastName = student.LastName,
                PlaceOfBirth = student.PlaceOfBirth,
                DateOfBirth = student.DateOfBirth,
                Status = false,
                InstituteID = student.InstituteID,
                GroupArhiveID = groupArchive.GroupArhiveID
            };

            _context.StudentArhives.Add(studentArchive);
            await _context.SaveChangesAsync();

            var marks = await _context.Marks
                .Where(m => m.StudentID == student.StudentID)
                .ToListAsync();

            var lessonIDs = marks.Select(m => m.LessonID).Distinct().ToList();
            var sourceLessons = (await _context.Lessons
                .Where(l => lessonIDs.Contains(l.LessonID))
                .ToListAsync())
                .ToDictionary(l => l.LessonID);

            List<(string LessonKey, LessonArhive Archive)> missingLessons = new();

            foreach (var mark in marks)
            {
                sourceLessons.TryGetValue(mark.LessonID, out Lesson lesson);

                string key = LessonKey(groupArchive.GroupArhiveID,
                    lesson?.ThemeID ?? mark.ThemeID,
                    lesson?.TypeOfExerciseID ?? mark.TypeOfExerciseID,
                    lesson?.Date ?? mark.Date,
                    lesson?.SubjectID ?? mark.SubjectID);

                if (maps.Lessons.ContainsKey(key) || missingLessons.Any(m => m.LessonKey == key))
                {
                    continue;
                }

                missingLessons.Add((key, BuildLessonArchive(groupArchive.GroupArhiveID, lesson, mark)));
            }

            await RegisterLessonArchivesAsync(groupArchive, maps, missingLessons);

            foreach (var mark in marks)
            {
                sourceLessons.TryGetValue(mark.LessonID, out Lesson lesson);

                string key = LessonKey(groupArchive.GroupArhiveID,
                    lesson?.ThemeID ?? mark.ThemeID,
                    lesson?.TypeOfExerciseID ?? mark.TypeOfExerciseID,
                    lesson?.Date ?? mark.Date,
                    lesson?.SubjectID ?? mark.SubjectID);

                maps.Lessons.TryGetValue(key, out int lessonArchiveID);

                _context.MarkArhives.Add(new MarkArhive
                {
                    Value = mark.Value,
                    Date = mark.Date,
                    Comment = mark.Comment,
                    SignatureOfTeacher = mark.SignatureOfTeacher,
                    HistoryOfMark = mark.HistoryOfMark,
                    FlagF = mark.FlagF,
                    InstituteID = mark.InstituteID,
                    SubjectID = mark.SubjectID,
                    // в архивных таблицах в этих столбцах лежат идентификаторы архивных
                    // записей группы и занятия, внешних ключей на Groups/Lessons в схеме нет
                    GroupID = groupArchive.GroupArhiveID,
                    LessonID = lessonArchiveID,
                    TypeOfExerciseID = mark.TypeOfExerciseID,
                    DepartmentID = mark.DepartmentID,
                    SpecialityID = mark.SpecialityID,
                    ThemeID = mark.ThemeID,
                    StudentArhiveID = studentArchive.StudentArhiveID
                });
            }

            await _context.SaveChangesAsync();

            await ArchiveJournalsAsync(group, groupArchive, maps, marks.Select(m => m.SubjectID));

            var statementMarks = await _context.StatementMarks
                .Where(m => m.StudentID == student.StudentID)
                .ToListAsync();

            var statementLessonIDs = statementMarks.Select(m => m.StatementLessonID).Distinct().ToList();
            var sourceStatementLessons = (await _context.StatementLessons
                .Where(l => statementLessonIDs.Contains(l.StatementLessonID))
                .ToListAsync())
                .ToDictionary(l => l.StatementLessonID);

            List<(string Key, StatementLessonArhive Archive)> missingStatementLessons = new();

            foreach (var mark in statementMarks)
            {
                sourceStatementLessons.TryGetValue(mark.StatementLessonID, out StatementLesson lesson);

                string key = StatementLessonKey(groupArchive.GroupArhiveID,
                    lesson?.TypeOfExerciseID ?? mark.TypeOfExerciseID,
                    lesson?.Date ?? mark.Date);

                if (maps.StatementLessons.ContainsKey(key) || missingStatementLessons.Any(s => s.Key == key))
                {
                    continue;
                }

                missingStatementLessons.Add((key, new StatementLessonArhive
                {
                    Date = lesson?.Date ?? mark.Date,
                    Comment = lesson?.Comment,
                    Signature = lesson?.Signature ?? mark.SignatureOfTeacher,
                    TypeOfExerciseID = lesson?.TypeOfExerciseID ?? mark.TypeOfExerciseID,
                    GroupArhiveID = groupArchive.GroupArhiveID
                }));
            }

            if (missingStatementLessons.Any())
            {
                _context.StatementLessonArhives.AddRange(missingStatementLessons.Select(s => s.Archive));
                await _context.SaveChangesAsync();

                foreach ((string key, StatementLessonArhive archive) in missingStatementLessons)
                {
                    maps.StatementLessons[key] = archive.StatementLessonArhiveID;
                }
            }

            foreach (var mark in statementMarks)
            {
                sourceStatementLessons.TryGetValue(mark.StatementLessonID, out StatementLesson lesson);

                string key = StatementLessonKey(groupArchive.GroupArhiveID,
                    lesson?.TypeOfExerciseID ?? mark.TypeOfExerciseID,
                    lesson?.Date ?? mark.Date);

                maps.StatementLessons.TryGetValue(key, out int statementLessonArchiveID);

                _context.StatementMarkArhives.Add(new StatementMarkArhive
                {
                    Value = mark.Value,
                    Date = mark.Date,
                    Comment = mark.Comment,
                    SignatureOfTeacher = mark.SignatureOfTeacher,
                    HistoryOfMark = mark.HistoryOfMark,
                    InstituteID = mark.InstituteID,
                    SpecialityID = mark.SpecialityID,
                    GroupID = groupArchive.GroupArhiveID,
                    TypeOfExerciseID = mark.TypeOfExerciseID,
                    StatementLessonID = statementLessonArchiveID,
                    StudentArhiveID = studentArchive.StudentArhiveID
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task RegisterLessonArchivesAsync(GroupArhive groupArchive, ArchiveMaps maps,
            IEnumerable<(string LessonKey, LessonArhive Archive)> lessons)
        {
            List<(string LessonKey, LessonArhive Archive)> newLessons = new();

            foreach ((string key, LessonArhive archive) in lessons)
            {
                if (maps.Lessons.ContainsKey(key) || newLessons.Any(l => l.LessonKey == key))
                {
                    continue;
                }

                newLessons.Add((key, archive));
            }

            if (!newLessons.Any())
            {
                return;
            }

            _context.LessonArhives.AddRange(newLessons.Select(l => l.Archive));
            await _context.SaveChangesAsync();

            foreach ((string key, LessonArhive archive) in newLessons)
            {
                maps.Lessons[key] = archive.LessonArhiveID;
            }
        }

        // Журналы нужны страницам просмотра архива, поэтому заводятся на каждый
        // предмет, по которому в архиве появились отметки
        private async Task ArchiveJournalsAsync(Group group, GroupArhive groupArchive, ArchiveMaps maps, IEnumerable<int> subjectIDs)
        {
            List<int> missingSubjects = subjectIDs
                .Distinct()
                .Where(subjectID => !maps.JournalSubjects.Contains(subjectID))
                .ToList();

            if (!missingSubjects.Any())
            {
                return;
            }

            var journals = await _context.Journals
                .Where(j => j.GroupID == group.GroupID && missingSubjects.Contains(j.SubjectID))
                .ToListAsync();

            foreach (int subjectID in missingSubjects)
            {
                var journal = journals.FirstOrDefault(j => j.SubjectID == subjectID);

                _context.JournalArhives.Add(new JournalArhive
                {
                    Comment = journal?.Comment,
                    Date = journal?.Date ?? DateTime.Now,
                    GroupArhiveID = groupArchive.GroupArhiveID,
                    SubjectID = subjectID
                });

                maps.JournalSubjects.Add(subjectID);
            }

            await _context.SaveChangesAsync();
        }

        private static LessonArhive BuildLessonArchive(int groupArchiveID, Lesson lesson)
        {
            return new LessonArhive
            {
                Date = lesson.Date,
                Comment = lesson.Comment,
                Signature = lesson.Signature,
                FlagF = lesson.FlagF,
                SubjectID = lesson.SubjectID,
                ThemeID = lesson.ThemeID,
                TypeOfExerciseID = lesson.TypeOfExerciseID,
                GroupArhiveID = groupArchiveID
            };
        }

        // Занятие могло быть удалено из рабочих таблиц — тогда данные берутся из отметки
        private static LessonArhive BuildLessonArchive(int groupArchiveID, Lesson lesson, Mark mark)
        {
            return new LessonArhive
            {
                Date = lesson?.Date ?? mark.Date,
                Comment = lesson?.Comment,
                Signature = lesson?.Signature ?? mark.SignatureOfTeacher,
                FlagF = lesson?.FlagF ?? mark.FlagF,
                SubjectID = lesson?.SubjectID ?? mark.SubjectID,
                ThemeID = lesson?.ThemeID ?? mark.ThemeID,
                TypeOfExerciseID = lesson?.TypeOfExerciseID ?? mark.TypeOfExerciseID,
                GroupArhiveID = groupArchiveID
            };
        }

        private static string LessonKey(int groupArchiveID, int themeID, int typeOfExerciseID, DateTime date, int subjectID)
        {
            return $"{groupArchiveID}|{themeID}|{typeOfExerciseID}|{date:yyyyMMddHHmmss}|{subjectID}";
        }

        private static string StatementLessonKey(int groupArchiveID, int typeOfExerciseID, DateTime date)
        {
            return $"{groupArchiveID}|{typeOfExerciseID}|{date:yyyyMMddHHmmss}";
        }

        // Исходные идентификаторы занятий в архивных таблицах не сохраняются, поэтому
        // соответствия строятся по составу: архивная группа, тема, тип и дата занятия
        private async Task<ArchiveMaps> LoadArchiveMapsAsync(int groupArchiveID)
        {
            ArchiveMaps maps = new();

            var lessons = await _context.LessonArhives
                .Where(l => l.GroupArhiveID == groupArchiveID)
                .Select(l => new { l.LessonArhiveID, l.ThemeID, l.TypeOfExerciseID, l.Date, l.SubjectID })
                .ToListAsync();

            foreach (var lesson in lessons)
            {
                maps.Lessons[LessonKey(groupArchiveID, lesson.ThemeID, lesson.TypeOfExerciseID, lesson.Date, lesson.SubjectID)] = lesson.LessonArhiveID;
            }

            var statementLessons = await _context.StatementLessonArhives
                .Where(l => l.GroupArhiveID == groupArchiveID)
                .Select(l => new { l.StatementLessonArhiveID, l.TypeOfExerciseID, l.Date })
                .ToListAsync();

            foreach (var lesson in statementLessons)
            {
                maps.StatementLessons[StatementLessonKey(groupArchiveID, lesson.TypeOfExerciseID, lesson.Date)] = lesson.StatementLessonArhiveID;
            }

            var journalSubjects = await _context.JournalArhives
                .Where(j => j.GroupArhiveID == groupArchiveID)
                .Select(j => j.SubjectID)
                .ToListAsync();

            foreach (int subjectID in journalSubjects)
            {
                maps.JournalSubjects.Add(subjectID);
            }

            return maps;
        }

        // Состояние одного архивирования: «исходные данные -> архивная запись»
        private sealed class ArchiveMaps
        {
            public Dictionary<string, int> Lessons { get; } = new Dictionary<string, int>();

            public Dictionary<string, int> StatementLessons { get; } = new Dictionary<string, int>();

            public HashSet<int> JournalSubjects { get; } = new HashSet<int>();
        }
    }
}
