using System.ComponentModel.DataAnnotations;

namespace HRHelper.Models;

public class JobPosition
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Description { get; set; } 
    public string? MustHave { get; set; }
    public string?  Technologies { get; set; } 
    public string?  InterviewGuide { get; set; }
    public string? Jargon { get; set; } 

    public List<Question> Questions { get; set; } = new();
}