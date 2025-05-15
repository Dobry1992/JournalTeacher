using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portal.Data;
using Portal.Models;
using Portal.Models.Election;
using Portal.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly NewsContext _context;
        private readonly AcademyContext _academyContext;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly UserNameService _userNameService;

        public HomeController(ILogger<HomeController> logger, NewsContext context, AcademyContext academyContext, IWebHostEnvironment appEnvironment, UserNameService userNameService)
        {
            _logger = logger;
            _context = context;
            _academyContext = academyContext;
            _appEnvironment = appEnvironment;
            _userNameService = userNameService;
        }

        public IActionResult Index()
        {
            string teacher = _userNameService.GetDisplayName();
            ViewBag.FullName = teacher;

            var birthdays = _context.Birthdays
                .Where(b => b.Date.Year == DateTime.Now.Year && b.Date.Month == DateTime.Now.Month && b.Date.Day == DateTime.Now.Day)
                .ToList();

            ViewBag.BNumber = birthdays.Count();
            if (birthdays.Any())
            {
                ViewBag.FirstBirthday = birthdays.First();
                birthdays.Remove(birthdays.First());
            }
            ViewBag.Birthdays = birthdays;

            var menus = _context.Menus;
            bool flagMenu = false;
            if (menus.Any())
            {
                ViewBag.Menu = menus.First();
                flagMenu = true;
            }
            ViewBag.FlagMenu = flagMenu;

            var news = _context.Articles
                .Include(n => n.Images)
                .OrderByDescending(n => n.DateOfNews);
            return View(news);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create()
        {
            string teacher = _userNameService.GetDisplayName();
            ViewBag.FullName = teacher;
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScheduleID,Name")] Schedule schedule, IFormFileCollection files)
        {
            if (ModelState.IsValid)
            {
                var schedules = _academyContext.Schedules;
                foreach (var s in schedules)
                {
                    _academyContext.Remove(s);
                }
                await _academyContext.SaveChangesAsync();

                foreach (var file in files)
                {
                    byte[] fileData = null;
                    using (var binaryReader = new BinaryReader(file.OpenReadStream()))
                    {
                        fileData = binaryReader.ReadBytes((int)file.Length);
                    }
                    schedule.File = fileData;
                    _academyContext.Schedules.Add(schedule);
                }
                await _academyContext.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Route("Расписание")]
        public async Task<FileContentResult> GetSchedule()
        {
            var schedule = await _academyContext.Schedules.FirstOrDefaultAsync();
            return File(schedule.File, "application/pdf");
        }

        public IActionResult IndexElection()
        {
            string teacher = _userNameService.GetDisplayName();
            ViewBag.FullName = teacher;

            var news = _context.ElectionArticles
                .Include(n => n.Images)
                .OrderByDescending(n => n.Date);
            return View(news);
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        public IActionResult CreateElectionArticle()
        {
            string teacher = _userNameService.GetDisplayName();
            ViewBag.FullName = teacher;

            return View();
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateElectionArticle([Bind("ElectionArticleID,Title,Text,Date")] ElectionArticle electionArticle, IFormFileCollection files)
        {
            if (ModelState.IsValid)
            {
                _context.Add(electionArticle);
                Article article = new()
                {
                    Title = electionArticle.Title,
                    Text = electionArticle.Text,
                    DateOfNews = electionArticle.Date
                };
                _context.Add(article);
                await _context.SaveChangesAsync();
                foreach (var file in files)
                {
                    string path = "/images/news/" + file.FileName;
                    using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    ElectionImage img = new()
                    {
                        ElectionArticleID = electionArticle.ElectionArticleID,
                        Title = electionArticle.Title,
                        Path = path
                    };
                    Image imgage = new()
                    {
                        ArticleID = article.ArticleID,
                        Title = article.Title,
                        Path = path
                    };
                    _context.Add(imgage);
                    _context.Add(img);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        public async Task<IActionResult> EditElection(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.ElectionArticles.FindAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            string teacher = _userNameService.GetDisplayName();
            ViewBag.FullName = teacher;

            return View(article);
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditElection(int id, [Bind("ElectionArticleID,Title,Text,Date")] ElectionArticle article)
        {
            if (id != article.ElectionArticleID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(article);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArticleExists(article.ElectionArticleID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        public async Task<IActionResult> DeleteElection(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.ElectionArticles
                .FirstOrDefaultAsync(m => m.ElectionArticleID == id);
            if (article == null)
            {
                return NotFound();
            }

            string teacher = _userNameService.GetDisplayName();
            ViewBag.FullName = teacher;

            return View(article);
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        [HttpPost, ActionName("DeleteElection")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteElectionConfirmed(int id)
        {
            var article = await _context.ElectionArticles.FindAsync(id);
            _context.ElectionArticles.Remove(article);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private bool ArticleExists(int id)
        {
            return _context.ElectionArticles.Any(e => e.ElectionArticleID == id);
        }
    }
}
