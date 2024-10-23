using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;

namespace Portal.Controllers
{
    public class TeacherNoPCsController : Controller
    {
        private readonly AcademyContext _context;

        public TeacherNoPCsController(AcademyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.TeacherNoPCs.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TeacherNoPCID,Name,Surname,LastName,Role")] TeacherNoPC teacherNoPC)
        {
            if (ModelState.IsValid)
            {
                _context.Add(teacherNoPC);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(teacherNoPC);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherNoPC = await _context.TeacherNoPCs.FindAsync(id);
            if (teacherNoPC == null)
            {
                return NotFound();
            }
            return View(teacherNoPC);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TeacherNoPCID,Name,Surname,LastName,Role")] TeacherNoPC teacherNoPC)
        {
            if (id != teacherNoPC.TeacherNoPCID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(teacherNoPC);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherNoPCExists(teacherNoPC.TeacherNoPCID))
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
            return View(teacherNoPC);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherNoPC = await _context.TeacherNoPCs
                .FirstOrDefaultAsync(m => m.TeacherNoPCID == id);
            if (teacherNoPC == null)
            {
                return NotFound();
            }

            return View(teacherNoPC);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacherNoPC = await _context.TeacherNoPCs.FindAsync(id);
            _context.TeacherNoPCs.Remove(teacherNoPC);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TeacherNoPCExists(int id)
        {
            return _context.TeacherNoPCs.Any(e => e.TeacherNoPCID == id);
        }
    }
}
