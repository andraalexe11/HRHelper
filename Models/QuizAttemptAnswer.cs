namespace HRHelper.Models;

public class QuizAttemptAnswer
{
    public int Id { get; set; }

    public int QuizAttemptId { get; set; }
    public QuizAttempt? QuizAttempt { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    public AnswerOption SelectedOption { get; set; }
    public bool IsCorrect { get; set; }
}
