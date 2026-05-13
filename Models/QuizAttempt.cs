using System;
using System.Collections.Generic;

namespace HRHelper.Models;

public class QuizAttempt
{
    public int Id { get; set; }

    public string RecruiterId { get; set; } = string.Empty;

    public int JobPositionId { get; set; }
    public JobPosition? JobPosition { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int Score { get; set; }
    public bool Passed { get; set; }

    public ICollection<QuizAttemptAnswer> Answers { get; set; } = new List<QuizAttemptAnswer>();
    public ICollection<QuizAttemptQuestion> SelectedQuestions { get; set; } = new List<QuizAttemptQuestion>();
}
