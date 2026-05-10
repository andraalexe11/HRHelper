using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRHelper.Models;
using HRHelper.Data;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var positions = await _context.JobPositions.ToListAsync();

        var counts = await _context.Questions
            .GroupBy(q => q.JobPositionId)
            .Select(g => new { JobPositionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPositionId, x => x.Count);

        foreach (var p in positions)
        {
            p.QuestionCount = counts.GetValueOrDefault(p.Id, 0);
        }

        return View(positions);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}