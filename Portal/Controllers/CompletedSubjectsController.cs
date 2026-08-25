using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using System.Linq;
using Portal.Models;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
    public class CompletedSubjectsController : Controller
    {
        private readonly AcademyContext _context;

        public CompletedSubjectsController(AcademyContext context)
        {
            _context = context;
        }

        // GET: CompletedSubjects/Create
        public IActionResult Create()
        {
            ViewData["GroupID"] = new SelectList(_context.Groups.OrderBy(g => g.Name), "GroupID", "Name");
            ViewData["SubjectID"] = new SelectList(_context.Subjects.OrderBy(s => s.Name), "SubjectID", "Name");
            return View();
        }

        // POST: CompletedSubjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SubjectID,GroupID")] CompletedSubject completedSubject)
        {
            if (ModelState.IsValid)
            {
                _context.Add(completedSubject);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }

            ViewData["GroupID"] = new SelectList(_context.Groups.OrderBy(g => g.Name), "GroupID", "Name", completedSubject.GroupID);
            ViewData["SubjectID"] = new SelectList(_context.Subjects.OrderBy(s => s.Name), "SubjectID", "Name", completedSubject.SubjectID);
            return View(completedSubject);
        }

        // GET: CompletedSubjects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var cs = await _context.CompletedSubjects
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cs == null)
                return NotFound();

            return View(cs);
        }

        // POST: CompletedSubjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cs = await _context.CompletedSubjects.FindAsync(id);
            if (cs != null)
            {
                _context.CompletedSubjects.Remove(cs);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
