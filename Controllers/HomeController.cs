using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using HRHelper.Data;
using HRHelper.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Controllers;

public class HomeController : Controller
{
    private const int MinQuestionsForQuiz = 5;

    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
        {
            return View("_AdminDashboard", await BuildAdminDashboardAsync());
        }

        if (User.IsInRole("Manager"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return View("_ManagerDashboard", await BuildManagerDashboardAsync(userId));
        }

        var recruiterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View("_RecruiterDashboard", await BuildRecruiterDashboardAsync(recruiterId));
    }

    public IActionResult Privacy() => View();

    private async Task<RecruiterDashboardVm> BuildRecruiterDashboardAsync(string userId)
    {
        var available = await _context.JobPositions
            .Where(p => p.Questions.Count >= MinQuestionsForQuiz)
            .OrderBy(p => p.Title)
            .Select(p => new PositionSummary
            {
                Id = p.Id,
                Title = p.Title!,
                Department = p.Department!
            })
            .ToListAsync();

        var attemptsQuery = _context.QuizAttempts.Where(a => a.RecruiterId == userId);

        var totalAttempts = await attemptsQuery.CountAsync(a => a.CompletedAt != null);
        var passedCount = await attemptsQuery.CountAsync(a => a.Passed);

        return new RecruiterDashboardVm
        {
            TotalAttempts = totalAttempts,
            PassedCount = passedCount,
            AvailablePositions = available
        };
    }

    private async Task<ManagerDashboardVm> BuildManagerDashboardAsync(string userId)
    {
        var positions = await _context.JobPositions
            .Where(p => p.CreatedById == userId)
            .OrderBy(p => p.Title)
            .Select(p => new PositionSummary
            {
                Id = p.Id,
                Title = p.Title!,
                Department = p.Department!,
                QuestionCount = p.Questions.Count,
                AttemptCount = _context.QuizAttempts.Count(a => a.JobPositionId == p.Id)
            })
            .ToListAsync();

        var needsAttention = positions
            .Where(p => p.QuestionCount < MinQuestionsForQuiz)
            .ToList();

        return new ManagerDashboardVm
        {
            MyPositions = positions,
            NeedsAttention = needsAttention
        };
    }

    private async Task<AdminDashboardVm> BuildAdminDashboardAsync()
    {
        var totalUsers = await _context.Users.CountAsync();

        var usersByRole = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            group ur by r.Name into g
            select new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role!, x => x.Count);

        var totalPositions = await _context.JobPositions.CountAsync();
        var totalQuestions = await _context.Questions.CountAsync();
        var totalAttempts = await _context.QuizAttempts.CountAsync(a => a.CompletedAt != null);

        var allPositions = await _context.JobPositions
            .OrderBy(p => p.Title)
            .Select(p => new PositionSummary
            {
                Id = p.Id,
                Title = p.Title!,
                Department = p.Department!,
                QuestionCount = p.Questions.Count,
                AttemptCount = _context.QuizAttempts.Count(a => a.JobPositionId == p.Id)
            })
            .ToListAsync();

        return new AdminDashboardVm
        {
            TotalUsers = totalUsers,
            UsersByRole = usersByRole,
            TotalPositions = totalPositions,
            TotalQuestions = totalQuestions,
            TotalAttempts = totalAttempts,
            AllPositions = allPositions
        };
    }
}
