using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRHelper.Models;

public class JobPosition
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string? Title { get; set; }

    [Required]
    [StringLength(100)]
    public string? Department { get; set; }

    [Required]
    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(4000)]
    public string? MustHave { get; set; }

    [StringLength(4000)]
    public string? Technologies { get; set; }

    [StringLength(4000)]
    public string? InterviewGuide { get; set; }

    [StringLength(4000)]
    public string? Jargon { get; set; }

    public string? CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? UpdatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<Question> Questions { get; set; } = new();

    [NotMapped]
    public int QuestionCount { get; set; }
}
