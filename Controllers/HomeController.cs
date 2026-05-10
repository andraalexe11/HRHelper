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

        return View(positions);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}