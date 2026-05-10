using System.Collections.Generic;

namespace HRHelper.Models;

public class TakeQuizViewModel
{
    public int AttemptId { get; set; }
    public string JobPositionTitle { get; set; } = string.Empty;
    public List<TakeQuizQuestionVm> Questions { get; set; } = new();
}

public class TakeQuizQuestionVm
{
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<TakeQuizOption> Options { get; set; } = new();
}

public class TakeQuizOption
{
    public AnswerOption Letter { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class QuizSubmitModel
{
    public List<QuizSubmitAnswer> Answers { get; set; } = new();
}

public class QuizSubmitAnswer
{
    public int QuestionId { get; set; }
    public AnswerOption? SelectedOption { get; set; }
}
