using System;
using System.ComponentModel.DataAnnotations;

namespace HRHelper.Models;

public enum AnswerOption
{
    A,
    B,
    C,
    D
}

public class Question
{
    public int Id { get; set; }

    public int JobPositionId { get; set; }

    [Required]
    [StringLength(500)]
    public string? Text { get; set; }

    [Required]
    [StringLength(300)]
    public string? OptionA { get; set; }

    [Required]
    [StringLength(300)]
    public string? OptionB { get; set; }

    [Required]
    [StringLength(300)]
    public string? OptionC { get; set; }

    [Required]
    [StringLength(300)]
    public string? OptionD { get; set; }

    public AnswerOption CorrectAnswer { get; set; }

    public string? CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? UpdatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public JobPosition? JobPosition { get; set; }
}
