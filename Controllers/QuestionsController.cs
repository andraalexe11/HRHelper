using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRHelper.Data;
using HRHelper.Models;

namespace HRHelper.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class QuestionsController : Controller
    {
        private const int MinQuestionsForQuiz = 5;

        private readonly ApplicationDbContext _context;

        public QuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("/JobPositions/{jobPositionId:int}/Questions")]
        public async Task<IActionResult> Index(int jobPositionId)
        {
            var position = await _context.JobPositions.FindAsync(jobPositionId);
            if (position is null)
            {
                return NotFound();
            }

            var questions = await _context.Questions
                .Where(q => q.JobPositionId == jobPositionId)
                .OrderBy(q => q.Id)
                .ToListAsync();

            ViewBag.JobPosition = position;
            ViewBag.MinQuestionsForQuiz = MinQuestionsForQuiz;
            return View(questions);
        }

        [HttpGet("/JobPositions/{jobPositionId:int}/Questions/Create")]
        public async Task<IActionResult> Create(int jobPositionId)
        {
            var position = await _context.JobPositions.FindAsync(jobPositionId);
            if (position is null)
            {
                return NotFound();
            }

            ViewBag.JobPosition = position;
            return View(new Question { JobPositionId = jobPositionId });
        }

        [HttpPost("/JobPositions/{jobPositionId:int}/Questions/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int jobPositionId, [Bind("JobPositionId,Text,OptionA,OptionB,OptionC,OptionD,CorrectAnswer")] Question question)
        {
            if (jobPositionId != question.JobPositionId)
            {
                return BadRequest();
            }

            var position = await _context.JobPositions.FindAsync(jobPositionId);
            if (position is null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                question.CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                question.CreatedAt = DateTime.UtcNow;

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { jobPositionId });
            }

            ViewBag.JobPosition = position;
            return View(question);
        }

        [HttpGet("/Questions/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var question = await _context.Questions
                .Include(q => q.JobPosition)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question is null)
            {
                return NotFound();
            }

            ViewBag.JobPosition = question.JobPosition;
            return View(question);
        }

        [HttpPost("/Questions/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,JobPositionId,Text,OptionA,OptionB,OptionC,OptionD,CorrectAnswer")] Question question)
        {
            if (id != question.Id)
            {
                return BadRequest();
            }

            var existing = await _context.Questions.FindAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                existing.Text = question.Text;
                existing.OptionA = question.OptionA;
                existing.OptionB = question.OptionB;
                existing.OptionC = question.OptionC;
                existing.OptionD = question.OptionD;
                existing.CorrectAnswer = question.CorrectAnswer;
                existing.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { jobPositionId = existing.JobPositionId });
            }

            ViewBag.JobPosition = await _context.JobPositions.FindAsync(existing.JobPositionId);
            return View(question);
        }

        [HttpPost("/Questions/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question is null)
            {
                return NotFound();
            }

            var jobPositionId = question.JobPositionId;
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { jobPositionId });
        }
    }
}
