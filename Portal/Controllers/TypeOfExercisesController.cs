using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;

namespace Portal.Controllers
{
    public class TypeOfExercisesController : Controller
    {
        private readonly AcademyContext _context;

        public TypeOfExercisesController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            List<TypeOfExercise> tps = new();
            var _types = from t in _context.Types
                         where t.Name == "Контрольное мероприятие" || t.Name == "Экзамен" || t.Name == "Дифференцированный зачёт" || t.Name == "Зачёт" || t.Name == "Итоговая оценка"
                         select t;
            foreach (TypeOfExercise t in _types)
            {
                tps.Add(t);
            }
            if (tps.Count == 0)
            {
                var types = new TypeOfExercise[]
                {
                    new TypeOfExercise{ Name = "Экзамен"},
                    new TypeOfExercise{ Name = "Дифференцированный зачёт"},
                    new TypeOfExercise{ Name = "Зачёт"},
                    new TypeOfExercise{ Name = "Итоговая оценка"},
                    new TypeOfExercise{ Name = "Курсовой проект"},
                    new TypeOfExercise{ Name = "Курсовая работа"},
                    new TypeOfExercise{ Name = "Лекция"},
                    new TypeOfExercise{ Name = "Лабораторное занятие"},
                    new TypeOfExercise{ Name = "Практическое занятие"},
                    new TypeOfExercise{ Name = "Семинарское занятие"},
                    new TypeOfExercise{ Name = "Учебная практика"},
                    new TypeOfExercise{ Name = "Производственная практика"},
                    new TypeOfExercise{ Name = "Стажировка"},
                    new TypeOfExercise{ Name = "Государственный экзамен"},
                    new TypeOfExercise{ Name = "Дипломная работа"},
                    new TypeOfExercise{ Name = "Дипломный проект"},
                    new TypeOfExercise{ Name = "Магистерская работа"},
                    new TypeOfExercise{ Name = "Контрольное мероприятие" }
                };
                foreach (TypeOfExercise _type in types)
                {
                    _context.Add(_type);
                }
                await _context.SaveChangesAsync();
            }

            return View(await _context.Types
                .OrderBy(t => t.Arch)
                    .ThenBy(t => t.Name)
                .ToListAsync());
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TypeOfExerciseID,Name,Color,ShortName")] TypeOfExercise typeOfExercise)
        {
            if (ModelState.IsValid)
            {
                _context.Add(typeOfExercise);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(typeOfExercise);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var typeOfExercise = await _context.Types.FindAsync(id);
            if (typeOfExercise == null)
            {
                return NotFound();
            }
            return View(typeOfExercise);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TypeOfExerciseID,Name,Color,Arch,ShortName")] TypeOfExercise typeOfExercise)
        {
            if (id != typeOfExercise.TypeOfExerciseID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(typeOfExercise);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TypeOfExerciseExists(typeOfExercise.TypeOfExerciseID))
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
            return View(typeOfExercise);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var typeOfExercise = await _context.Types
                .FirstOrDefaultAsync(m => m.TypeOfExerciseID == id);
            if (typeOfExercise == null)
            {
                return NotFound();
            }

            return View(typeOfExercise);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var typeOfExercise = await _context.Types.FindAsync(id);
            _context.Types.Remove(typeOfExercise);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var typeOfExercise = await _context.Types
                .FirstOrDefaultAsync(d => d.TypeOfExerciseID == id);

            if (typeOfExercise == null)
            {
                return NotFound();
            }

            return View(typeOfExercise);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, [Bind("TypeOfExerciseID,Name,Color,Arch,ShortName")] TypeOfExercise typeOfExercise)
        {
            if (id != typeOfExercise.TypeOfExerciseID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (typeOfExercise.Arch == true)
                    {
                        typeOfExercise.Arch = false;
                    }
                    else
                    {
                        typeOfExercise.Arch = true;
                    }
                    _context.Update(typeOfExercise);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (!TypeOfExerciseExists(typeOfExercise.TypeOfExerciseID))
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
            return View(typeOfExercise);
        }

        private bool TypeOfExerciseExists(int id)
        {
            return _context.Types.Any(e => e.TypeOfExerciseID == id);
        }
    }
}
