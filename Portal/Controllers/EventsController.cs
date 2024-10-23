using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Portal
{
    public class EventsController : Controller
    {
        private readonly AcademyContext _context;

        public EventsController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index(DateTime date)
        {
            var events = _context.Events
                 .Where(e => e.Date.Year == DateTime.Now.Year && e.Date.Month == DateTime.Now.Month && e.Date.Day == DateTime.Now.Day)
                 .OrderByDescending(e => e.Date);

            if (date.ToShortDateString() != "01.01.0001")
            {
                events = _context.Events
                    .Where(e => e.Date.Year == date.Year && e.Date.Month == date.Month && e.Date.Day == date.Day)
                    .OrderByDescending(e => e.Date);
            }
            return View(await events.ToListAsync());
        }
    }
}
