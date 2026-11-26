using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    // Комментарии о курсантах/слушателях. Доступ только у SuperAdmin и ANB-UMCH:
    // добавление/удаление на странице курсанта и выдача текстов для всплывающих подсказок.
    [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
    public class StudentCommentsController : Controller
    {
        private const int MaxIdsPerRequest = 2000;

        private readonly AcademyContext _context;
        private readonly UserNameService _userNameService;

        public StudentCommentsController(AcademyContext context, UserNameService userNameService)
        {
            _context = context;
            _userNameService = userNameService;
        }

        // Данные для всплывающих подсказок: { "<StudentID>": "<html>" }
        // Возвращаются только курсанты, у которых есть комментарии
        public async Task<IActionResult> ForStudents(string ids)
        {
            var studentIds = (ids ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .Take(MaxIdsPerRequest)
                .ToList();

            if (studentIds.Count == 0)
            {
                return Json(new Dictionary<string, string>());
            }

            var comments = await _context.Comments
                .AsNoTracking()
                .Where(c => studentIds.Contains(c.StudentID))
                .OrderBy(c => c.Date)
                .ToListAsync();

            var result = comments
                .GroupBy(c => c.StudentID)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => string.Join("<hr class='my-1' />", g.Select(c =>
                        $"<b>{c.Date:dd.MM.yyyy}</b> {WebUtility.HtmlEncode(c.Author)}<br />" +
                        WebUtility.HtmlEncode(c.CommentString))));

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int studentId, string comment)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] = "Комментарий не может быть пустым.";
                return RedirectToAction("Details", "Students", new { id = studentId });
            }

            _context.Comments.Add(new Comment
            {
                StudentID = studentId,
                CommentString = comment.Trim(),
                Date = DateTime.Now,
                Author = _userNameService.GetDisplayName()
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Комментарий добавлен.";
            return RedirectToAction("Details", "Students", new { id = studentId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int commentId, int studentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Комментарий удален.";
            }

            return RedirectToAction("Details", "Students", new { id = studentId });
        }
    }
}
