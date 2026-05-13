using System.Linq;
using System.Threading.Tasks;
using HRHelper.Data;
using HRHelper.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class ActivityController : Controller
{
    private const int RecentLimit = 100;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivityController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("/Activity")]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var attemptsQ = _context.QuizAttempts.Where(a => a.CompletedAt != null);
        if (!isAdmin)
        {
            attemptsQ = attemptsQ.Where(a => a.JobPosition!.CreatedById == userId);
        }

        var items = await (
            from a in attemptsQ
            join p in _context.JobPositions on a.JobPositionId equals p.Id
            join u in _context.Users on a.RecruiterId equals u.Id
            orderby a.CompletedAt descending
            select new AttemptSummary
            {
                AttemptId = a.Id,
                PositionTitle = p.Title!,
                RecruiterEmail = u.Email,
                Score = a.Score,
                Passed = a.Passed,
                StartedAt = a.StartedAt,
                CompletedAt = a.CompletedAt
            })
            .Take(RecentLimit)
            .ToListAsync();

        ViewBag.IsAdmin = isAdmin;
        ViewBag.RecentLimit = RecentLimit;
        return View(items);
    }
}
