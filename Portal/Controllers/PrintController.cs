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
            // ClosedXML

            var lessons = _context.Lessons
                .Include(l => l.TypeOfExercise)
                .Include(l => l.Theme)
                .OrderBy(l => l.Date)
                .Where(l => l.GroupID == groupID && l.SubjectID == subjectID)
                .AsNoTracking();

            var students = _context.Students
                .OrderBy(s => s.LastName)
                .Where(s => s.GroupID == groupID)
                .AsNoTracking();

            var marks = _context.Marks
                .OrderBy(m => m.Date)
                .Where(m => m.GroupID == groupID && m.SubjectID == subjectID)
                .AsNoTracking();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Журнал");
            var currentRow = 1;

            worksheet.Cell(1, 1).SetValue("№ п/п").Style.Font.SetFontName("Times New Roman")
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            worksheet.Cell(1, 2).SetValue("Ф.И.О.").Style.Font.SetFontName("Times New Roman")
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            int i = 3;
            int n = 1;
            foreach (var lesson in lessons)
            {
                var cell = worksheet.Cell(currentRow, i);

                cell.Style.Font.SetFontName("Times New Roman")
                    .Alignment.SetTextRotation(90)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.WrapText = true;

                cell.CreateRichText()
                   .AddText(lesson.Date.ToShortDateString()).AddNewLine()
                   .AddText(lesson.Theme.ShortName).AddNewLine()
                   .AddText(lesson.TypeOfExercise.Name);

                i++;
            }

            foreach (var student in students)
            {
                int j = 3;
                currentRow++;
                worksheet.Cell(currentRow, 1).SetValue(n.ToString()).Style.Font.SetFontName("Times New Roman")
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center); ;
                worksheet.Cell(currentRow, 2).SetValue($"{student.LastName} {student.Name} {student.Surname}").Style.Font.SetFontName("Times New Roman")
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center); ;
                n++;
                foreach (var mark in marks)
                {
                    if (mark.StudentID == student.StudentID)
                    {
                        worksheet.Cell(currentRow, j).SetValue(mark.Value).Style.Font.SetFontName("Times New Roman")
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Alignment.SetVertical(XLAlignmentVerticalValues.Center); ;
                        j++;
                    }
                }
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
            var students = _context.Students
                .Where(s => s.GroupID == lesson.GroupID)
                .OrderBy(s => s.LastName)
                .AsNoTracking();
            var institute = await _context.Institutes.FindAsync((students.FirstOrDefault().InstituteID));
            var group = await _context.Groups.FindAsync(lesson.GroupID);
            var subject = await _context.Subjects.FindAsync(lesson.SubjectID);
            var marks = _context.Marks
                 .Where(m => m.LessonID == lesson.LessonID)
                 .AsNoTracking();
            int i = 1;


            string course = "";
            int c1 = DateTime.Now.Year - group.DateEnter.Year;
            string year = "";
            string term = "";
            if (DateTime.Now.Month.ToString() == "9" || DateTime.Now.Month.ToString() == "10" || DateTime.Now.Month.ToString() == "11" || DateTime.Now.Month.ToString() == "12")
            {
                year = DateTime.Now.Year.ToString() + "/" + DateTime.Now.AddYears(1).Year.ToString();
                term = "1-й семестр";
                course = (c1 + 1).ToString() + " курс";
            }
            else
            {
                year = DateTime.Now.AddYears(-1).Year.ToString() + "/" + DateTime.Now.Year.ToString();
                term = "2-й семестр";
                course = c1.ToString() + " курс";
            }
            if (c1 == 0)
            {
                course = "1 курс";
            }

            int studentsNumber = 0;
            foreach (var m in marks)
            {
                if (int.TryParse(m.Value, out var number))
                {
                    studentsNumber++;
                }
            }

            var dt = new DataTable();
            dt.Columns.Add("StudentID");
            dt.Columns.Add("Student");
            dt.Columns.Add("GradebookNumber");
            dt.Columns.Add("MarkTest");
            dt.Columns.Add("MarkNumber");
            //dt.Columns.Add("FinalMark");
            dt.Columns.Add("Signature");

            DataRow row;
            foreach (var s in students)
            {
                row = dt.NewRow();
                row["StudentID"] = i;
                row["Student"] = s.LastName + " " + s.Name[0] + "." + s.Surname[0] + ".";
                row["GradebookNumber"] = "";
                foreach (var m in marks)
                {
                    if (m.StudentID == s.StudentID && int.TryParse(m.Value, out var number) && type.Name == "Зачёт")
                    {
                        if (number >= 4)
                        {
                            row["MarkTest"] = "зачтено";
                            row["MarkNumber"] = m.Value;
                        }
                        else
                        {
                            row["MarkTest"] = "не зачтено";
                            row["MarkNumber"] = m.Value;
                        }

                    }
                    else if (m.StudentID == s.StudentID)
                    {
                        row["MarkTest"] = "";
                        row["MarkNumber"] = m.Value;
                    }
                }
                row["Signature"] = "";
                dt.Rows.Add(row);
                i++;
            }

            string mimetype = "";
            int extension = 1;
            var path = $"{this._webHostEnvironment.WebRootPath}\\Reports\\rptStatement.rdlc";
            Dictionary<string, string> parameters = new();

            var groupMarks = marks.GroupBy(m => m.Value).Select(v => new { Value = v.Key, Count = v.Count() });
            string[] valueMarks = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
            for (int j = 0; j < valueMarks.Length; j++)
            {
                foreach (var m in groupMarks)
                {
                    if (m.Value == valueMarks[j])
                    {
                        parameters.Add("v" + valueMarks[j].ToString(), m.Count.ToString());
                    }
                }
                if (!parameters.ContainsKey("v" + valueMarks[j].ToString()))
                {
                    parameters.Add("v" + valueMarks[j].ToString(), "-");
                }
            }

            parameters.Add("prm", type.Name.ToString());
            parameters.Add("institute", institute.Name.ToString());
            parameters.Add("group", group.Name.ToString());
            parameters.Add("subject", subject.Name.ToString());
            parameters.Add("date", lesson.Date.ToShortDateString());
            parameters.Add("year", year);
            parameters.Add("term", term);
            parameters.Add("course", course);
            parameters.Add("studentsNumber", studentsNumber.ToString());
            parameters.Add("studentsAbsent", (i - studentsNumber - 1).ToString());
            LocalReport localReport = new(path);
            localReport.AddDataSource("dsStatement", dt);
            var result = localReport.Execute(RenderType.Word, extension, parameters, mimetype);
            return File(result.MainStream, "application/msword");
        }
    }
}
