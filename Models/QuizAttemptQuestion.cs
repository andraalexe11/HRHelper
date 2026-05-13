namespace HRHelper.Models;

public class QuizAttemptQuestion
{
    public int Id { get; set; }

    public int QuizAttemptId { get; set; }
    public QuizAttempt? QuizAttempt { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    public int DisplayOrder { get; set; }
}
