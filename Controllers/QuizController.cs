using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRHelper.Data;
using HRHelper.Models;

namespace HRHelper.Controllers
{
    [Authorize(Roles = "Recruiter,Admin")]
    public class QuizController : Controller
    {
        private const int QuestionsPerQuiz = 3;
        private const int MinQuestionsRequired = 5;

        private readonly ApplicationDbContext _context;

        public QuizController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("/Quiz/Start/{jobPositionId:int}")]
        public async Task<IActionResult> Start(int jobPositionId)
        {
            var position = await _context.JobPositions.FindAsync(jobPositionId);
            if (position is null)
            {
                return NotFound();
            }

            var questionIds = await _context.Questions
                .Where(q => q.JobPositionId == jobPositionId)
                .Select(q => q.Id)
                .ToListAsync();

            if (questionIds.Count < MinQuestionsRequired)
            {
                ViewBag.JobPosition = position;
                ViewBag.AvailableQuestions = questionIds.Count;
                ViewBag.MinRequired = MinQuestionsRequired;
                return View("NotReady");
            }

            var picked = PickRandomDistinct(questionIds, QuestionsPerQuiz);

            var attempt = new QuizAttempt
            {
                RecruiterId = CurrentUserId(),
                JobPositionId = jobPositionId,
                StartedAt = DateTime.UtcNow,
                Score = 0,
                Passed = false
            };

            for (int i = 0; i < picked.Count; i++)
            {
                attempt.SelectedQuestions.Add(new QuizAttemptQuestion
                {
                    QuestionId = picked[i],
                    DisplayOrder = i + 1
                });
            }

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Take), new { attemptId = attempt.Id });
        }

        [HttpGet("/Quiz/Take/{attemptId:int}")]
        public async Task<IActionResult> Take(int attemptId)
        {
            var attempt = await LoadOwnedAttemptAsync(attemptId);
            if (attempt is null)
            {
                return NotFound();
            }

            if (attempt.CompletedAt is not null)
            {
                return RedirectToAction(nameof(Result), new { attemptId });
            }

            return View(BuildTakeViewModel(attempt));
        }

        [HttpPost("/Quiz/Submit/{attemptId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int attemptId, QuizSubmitModel model)
        {
            var attempt = await LoadOwnedAttemptAsync(attemptId);
            if (attempt is null)
            {
                return NotFound();
            }

            if (attempt.CompletedAt is not null)
            {
                return RedirectToAction(nameof(Result), new { attemptId });
            }

            var expectedIds = attempt.SelectedQuestions
                .Select(s => s.QuestionId)
                .ToHashSet();

            var submitted = (model?.Answers ?? new List<QuizSubmitAnswer>()).ToList();

            bool valid = submitted.Count == expectedIds.Count
                         && submitted.All(a => a.SelectedOption.HasValue)
                         && submitted.Select(a => a.QuestionId).ToHashSet().SetEquals(expectedIds);

            if (!valid)
            {
                ModelState.AddModelError(string.Empty, "Please answer all 3 questions before submitting.");
                return View("Take", BuildTakeViewModel(attempt));
            }

            int score = 0;
            foreach (var ans in submitted)
            {
                var question = attempt.SelectedQuestions.First(s => s.QuestionId == ans.QuestionId).Question!;
                bool isCorrect = ans.SelectedOption!.Value == question.CorrectAnswer;
                if (isCorrect)
                {
                    score++;
                }

                attempt.Answers.Add(new QuizAttemptAnswer
                {
                    QuestionId = ans.QuestionId,
                    SelectedOption = ans.SelectedOption.Value,
                    IsCorrect = isCorrect
                });
            }

            attempt.Score = score;
            attempt.Passed = score == QuestionsPerQuiz;
            attempt.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Result), new { attemptId });
        }

        [HttpGet("/Quiz/Result/{attemptId:int}")]
        public async Task<IActionResult> Result(int attemptId)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.JobPosition)
                .Include(a => a.SelectedQuestions).ThenInclude(s => s.Question)
                .Include(a => a.Answers).ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt is null || attempt.RecruiterId != CurrentUserId())
            {
                return NotFound();
            }

            if (attempt.CompletedAt is null)
            {
                return RedirectToAction(nameof(Take), new { attemptId });
            }

            return View(attempt);
        }

        [HttpGet("/Quiz/MyAttempts")]
        public async Task<IActionResult> MyAttempts()
        {
            var userId = CurrentUserId();
            var attempts = await _context.QuizAttempts
                .Where(a => a.RecruiterId == userId)
                .Include(a => a.JobPosition)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();

            return View(attempts);
        }

        private async Task<QuizAttempt?> LoadOwnedAttemptAsync(int attemptId)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.SelectedQuestions).ThenInclude(s => s.Question)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt is null || attempt.RecruiterId != CurrentUserId())
            {
                return null;
            }

            return attempt;
        }

        private TakeQuizViewModel BuildTakeViewModel(QuizAttempt attempt)
        {
            var position = _context.JobPositions.Find(attempt.JobPositionId);

            var questions = attempt.SelectedQuestions
                .OrderBy(s => s.DisplayOrder)
                .Select(sq =>
                {
                    var q = sq.Question!;
                    var options = new List<TakeQuizOption>
                    {
                        new() { Letter = AnswerOption.A, Text = q.OptionA ?? string.Empty },
                        new() { Letter = AnswerOption.B, Text = q.OptionB ?? string.Empty },
                        new() { Letter = AnswerOption.C, Text = q.OptionC ?? string.Empty },
                        new() { Letter = AnswerOption.D, Text = q.OptionD ?? string.Empty }
                    };
                    ShuffleInPlace(options);

                    return new TakeQuizQuestionVm
                    {
                        QuestionId = q.Id,
                        Text = q.Text ?? string.Empty,
                        DisplayOrder = sq.DisplayOrder,
                        Options = options
                    };
                })
                .ToList();

            return new TakeQuizViewModel
            {
                AttemptId = attempt.Id,
                JobPositionTitle = position?.Title ?? string.Empty,
                Questions = questions
            };
        }

        private static List<int> PickRandomDistinct(List<int> source, int count)
        {
            var arr = source.ToArray();
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr.Take(count).ToList();
        }

        private static void ShuffleInPlace<T>(IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
