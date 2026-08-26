using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class CompletedSubjectsController : Controller
    {
        private readonly AcademyContext _context;

        public CompletedSubjectsController(AcademyContext context)
        {
            _context = context;
        }

        // GET: для отображения формы
        public async Task<IActionResult> Create(int SubjectID, int GroupID, string returnUrl = null)
        {
            var subject = await _context.Subjects.FindAsync(SubjectID);
            var group = await _context.Groups.FindAsync(GroupID);

            if (subject == null || group == null)
            {
                return NotFound();
            }

            ViewData["SubjectName"] = subject.Name;
            ViewData["GroupName"] = group.Name;
            ViewData["GroupID"] = GroupID;
            ViewData["SubjectID"] = SubjectID;

            // Сохраняем URL для возврата
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Request.Headers["Referer"].ToString();
            }
            ViewData["ReturnUrl"] = returnUrl;

            var model = new CompletedSubject
            {
                SubjectID = SubjectID,
                GroupID = GroupID
            };

            return View(model);
        }

        // POST: CompletedSubjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SubjectID,GroupID")] CompletedSubject completedSubject, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var exists = await _context.CompletedSubjects
                    .AnyAsync(cs => cs.SubjectID == completedSubject.SubjectID && cs.GroupID == completedSubject.GroupID);

                if (!exists)
                {
                    _context.Add(completedSubject);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Предмет успешно завершен!";
                }
                else
                {
                    TempData["Error"] = "Этот предмет уже завершен для данной группы!";
                }

                // Возвращаемся на предыдущую страницу
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("AdjustedJournal", "Journals", new
                {
                    GroupID = completedSubject.GroupID,
                    SubjectID = completedSubject.SubjectID
                });
            }

            return View(completedSubject);
        }

        // GET: CompletedSubjects/Delete?SubjectID=...&GroupID=...
        public async Task<IActionResult> Delete(int SubjectID, int GroupID)
        {
            var cs = await _context.CompletedSubjects
                .FirstOrDefaultAsync(m => m.SubjectID == SubjectID && m.GroupID == GroupID);
            if (cs == null)
                return NotFound();

            var subject = await _context.Subjects.FindAsync(SubjectID);
            var group = await _context.Groups.FindAsync(GroupID);

            ViewData["SubjectName"] = subject?.Name ?? "Неизвестно";
            ViewData["GroupName"] = group?.Name ?? "Неизвестно";
            ViewData["GroupID"] = GroupID;
            ViewData["SubjectID"] = SubjectID;
            ViewData["ReturnUrl"] = Request.Headers["Referer"].ToString();

            return View(cs);
        }

        // POST: CompletedSubjects/Delete (by SubjectID and GroupID)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int SubjectID, int GroupID, string returnUrl)
        {
            var cs = await _context.CompletedSubjects
                .FirstOrDefaultAsync(m => m.SubjectID == SubjectID && m.GroupID == GroupID);
            if (cs != null)
            {
                _context.CompletedSubjects.Remove(cs);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Предмет успешно продолжен!";
            }

            // Возвращаемся в журнал
            return RedirectToAction("AdjustedJournal", "Journals", new
            {
                GroupID = GroupID,
                SubjectID = SubjectID
            });
        }
    }
}
