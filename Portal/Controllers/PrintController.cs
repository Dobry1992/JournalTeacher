using AspNetCore.Reporting;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class PrintController : Controller
    {
        private readonly AcademyContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PrintController(IWebHostEnvironment webHostEnvironment, AcademyContext context)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public IActionResult PrintJournal(int groupID, int subjectID)
        {
            var lessons = _context.Lessons
                .Include(l => l.TypeOfExercise)
                .Include(l => l.Theme)
                .OrderBy(l => l.Date)
                .Where(l => l.GroupID == groupID && l.SubjectID == subjectID)
                .AsNoTracking()
                .ToList();

            var students = _context.Students
                .OrderBy(s => s.LastName)
                .Where(s => s.GroupID == groupID)
                .AsNoTracking()
                .ToList();

            var marks = _context.Marks
                .OrderBy(m => m.Date)
                .Where(m => m.GroupID == groupID && m.SubjectID == subjectID)
                .AsNoTracking()
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Журнал");
            var currentRow = 1;

            worksheet.Cell(1, 1).Value = "№ п/п";
            worksheet.Cell(1, 2).Value = "Ф.И.О.";

            worksheet.Range(1, 1, 1, 2).Style.Font.SetFontName("Times New Roman")
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            int column = 3;
            foreach (var lesson in lessons)
            {
                var cell = worksheet.Cell(currentRow, column);
                cell.Value = $"{lesson.Date:dd.MM.yyyy}\n{lesson.Theme.ShortName}\n{lesson.TypeOfExercise.Name}";
                cell.Style.Font.SetFontName("Times New Roman");
                cell.Style.Alignment.SetTextRotation(90);
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                cell.Style.Alignment.WrapText = true;
                column++;
            }

            int row = 2;
            int number = 1;
            foreach (var student in students)
            {
                worksheet.Cell(row, 1).Value = number++;
                worksheet.Cell(row, 2).Value = $"{student.LastName} {student.Name} {student.Surname}";

                worksheet.Cell(row, 1).Style.Font.SetFontName("Times New Roman");
                worksheet.Cell(row, 2).Style.Font.SetFontName("Times New Roman");

                column = 3;
                foreach (var lesson in lessons)
                {
                    var mark = marks.FirstOrDefault(m => m.StudentID == student.StudentID && m.LessonID == lesson.LessonID);
                    worksheet.Cell(row, column).Value = mark?.Value ?? string.Empty;
                    worksheet.Cell(row, column).Style.Font.SetFontName("Times New Roman");
                    worksheet.Cell(row, column).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    worksheet.Cell(row, column).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                    column++;
                }

                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "журнал.xlsx");
        }

        public async Task<IActionResult> PrintStatement(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            var type = await _context.Types.FindAsync(lesson.TypeOfExerciseID);
            var students = await _context.Students
                .Where(s => s.GroupID == lesson.GroupID)
                .OrderBy(s => s.LastName)
                .ToListAsync();

            var institute = await _context.Institutes.FindAsync(students.First().InstituteID);
            var group = await _context.Groups.FindAsync(lesson.GroupID);
            var subject = await _context.Subjects.FindAsync(lesson.SubjectID);
            var marks = await _context.Marks
                .Where(m => m.LessonID == lesson.LessonID)
                .ToListAsync();

            var dt = new DataTable();
            dt.Columns.Add("StudentID");
            dt.Columns.Add("Student");
            dt.Columns.Add("GradebookNumber");
            dt.Columns.Add("MarkTest");
            dt.Columns.Add("MarkNumber");
            dt.Columns.Add("Signature");

            int i = 1;
            int studentsPassed = 0;

            foreach (var s in students)
            {
                var row = dt.NewRow();
                row["StudentID"] = i++;
                row["Student"] = $"{s.LastName} {s.Name[0]}.{s.Surname[0]}.";
                row["GradebookNumber"] = string.Empty;

                var mark = marks.FirstOrDefault(m => m.StudentID == s.StudentID);
                if (mark != null && int.TryParse(mark.Value, out int markValue))
                {
                    if (type.Name == "Зачёт")
                    {
                        row["MarkTest"] = markValue >= 4 ? "зачтено" : "не зачтено";
                        row["MarkNumber"] = mark.Value;
                    }
                    else
                    {
                        row["MarkTest"] = string.Empty;
                        row["MarkNumber"] = mark.Value;
                    }
                    studentsPassed++;
                }
                else
                {
                    row["MarkTest"] = string.Empty;
                    row["MarkNumber"] = string.Empty;
                }

                row["Signature"] = string.Empty;
                dt.Rows.Add(row);
            }

            var parameters = new Dictionary<string, string>();
            var groupedMarks = marks.GroupBy(m => m.Value).ToDictionary(g => g.Key, g => g.Count());
            string[] valueMarks = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };

            foreach (var val in valueMarks)
            {
                parameters[$"v{val}"] = groupedMarks.TryGetValue(val, out int count) ? count.ToString() : "-";
            }

            var now = DateTime.Now;
            string year = now.Month >= 9 ? $"{now.Year}/{now.Year + 1}" : $"{now.Year - 1}/{now.Year}";
            string term = now.Month >= 9 ? "1-й семестр" : "2-й семестр";
            int course = now.Month >= 9 ? now.Year - group.DateEnter.Year + 1 : now.Year - group.DateEnter.Year;
            course = course == 0 ? 1 : course;

            parameters.Add("prm", type.Name);
            parameters.Add("institute", institute.Name);
            parameters.Add("group", group.Name);
            parameters.Add("subject", subject.Name);
            parameters.Add("date", lesson.Date.ToShortDateString());
            parameters.Add("year", year);
            parameters.Add("term", term);
            parameters.Add("course", $"{course} курс");
            parameters.Add("studentsNumber", studentsPassed.ToString());
            parameters.Add("studentsAbsent", (students.Count - studentsPassed).ToString());

            string path = Path.Combine(_webHostEnvironment.WebRootPath, "Reports", "rptStatement.rdlc");
            LocalReport report = new(path);
            report.AddDataSource("dsStatement", dt);
            var result = report.Execute(RenderType.Word, 1, parameters);

            return File(result.MainStream, "application/msword", "statement.doc");
        }
    }
}
