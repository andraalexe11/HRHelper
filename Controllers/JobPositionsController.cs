using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HRHelper.Data;
using HRHelper.Models;

namespace HRHelper.Controllers
{
    [Authorize]
    public class JobPositionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobPositionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? q, string? department)
        {
            IQueryable<JobPosition> query = _context.JobPositions;

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLower = q.Trim().ToLower();
                query = query.Where(p => (p.Title ?? "").ToLower().Contains(qLower));
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(p => p.Department == department);
            }

            var positions = await query
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Select(p => new JobPositionListItem
                {
                    Id = p.Id,
                    Title = p.Title!,
                    Department = p.Department!,
                    QuestionCount = p.Questions.Count,
                    AttemptCount = _context.QuizAttempts.Count(a => a.JobPositionId == p.Id),
                    CreatedAt = p.CreatedAt,
                    CreatedById = p.CreatedById,
                    UpdatedAt = p.UpdatedAt,
                    UpdatedById = p.UpdatedById
                })
                .ToListAsync();

            var ids = positions
                .Select(p => p.CreatedById)
                .Concat(positions.Select(p => p.UpdatedById))
                .Where(id => !string.IsNullOrEmpty(id))
                .Cast<string>()
                .Distinct()
                .ToList();

            var emailMap = await _context.Users
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email);

            foreach (var p in positions)
            {
                if (!string.IsNullOrEmpty(p.CreatedById))
                {
                    p.CreatedByEmail = emailMap.GetValueOrDefault(p.CreatedById);
                }
                if (!string.IsNullOrEmpty(p.UpdatedById))
                {
                    p.UpdatedByEmail = emailMap.GetValueOrDefault(p.UpdatedById);
                }
            }

            ViewBag.Q = q;
            ViewBag.Department = department;
            return View(positions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jobPosition = await _context.JobPositions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (jobPosition == null)
            {
                return NotFound();
            }

            jobPosition.QuestionCount = await _context.Questions
                .CountAsync(q => q.JobPositionId == jobPosition.Id);

            var ids = new[] { jobPosition.CreatedById, jobPosition.UpdatedById }
                .Where(s => !string.IsNullOrEmpty(s))
                .Cast<string>()
                .Distinct()
                .ToList();

            if (ids.Count > 0)
            {
                var emailMap = await _context.Users
                    .Where(u => ids.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.Email);

                if (!string.IsNullOrEmpty(jobPosition.CreatedById))
                {
                    jobPosition.CreatedByEmail = emailMap.GetValueOrDefault(jobPosition.CreatedById);
                }
                if (!string.IsNullOrEmpty(jobPosition.UpdatedById))
                {
                    jobPosition.UpdatedByEmail = emailMap.GetValueOrDefault(jobPosition.UpdatedById);
                }
            }

            return View(jobPosition);
        }

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([Bind("Id,Title,Department,Description,MustHave,Technologies,InterviewGuide,Jargon")] JobPosition jobPosition)
        {
            if (ModelState.IsValid)
            {
                jobPosition.CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                jobPosition.CreatedAt = DateTime.UtcNow;

                _context.Add(jobPosition);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }
            return View(jobPosition);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jobPosition = await _context.JobPositions.FindAsync(id);
            if (jobPosition == null)
            {
                return NotFound();
            }
            return View(jobPosition);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Department,Description,MustHave,Technologies,InterviewGuide,Jargon")] JobPosition jobPosition)
        {
            if (id != jobPosition.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.JobPositions.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.Title = jobPosition.Title;
                    existing.Department = jobPosition.Department;
                    existing.Description = jobPosition.Description;
                    existing.MustHave = jobPosition.MustHave;
                    existing.Technologies = jobPosition.Technologies;
                    existing.InterviewGuide = jobPosition.InterviewGuide;
                    existing.Jargon = jobPosition.Jargon;
                    existing.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    existing.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobPositionExists(jobPosition.Id))
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
            return View(jobPosition);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jobPosition = await _context.JobPositions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (jobPosition == null)
            {
                return NotFound();
            }

            return View(jobPosition);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var jobPosition = await _context.JobPositions.FindAsync(id);
            if (jobPosition != null)
            {
                _context.JobPositions.Remove(jobPosition);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool JobPositionExists(int id)
        {
            return _context.JobPositions.Any(e => e.Id == id);
        }
    }
}
