using System;
using System.Collections.Generic;

namespace HRHelper.Models;

public class PositionSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int AttemptCount { get; set; }
}

public class JobPositionListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? CreatedByEmail { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedById { get; set; }
    public string? UpdatedByEmail { get; set; }
}

public class AttemptSummary
{
    public int AttemptId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public string? RecruiterEmail { get; set; }
    public int Score { get; set; }
    public bool Passed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class RecruiterDashboardVm
{
    public int TotalAttempts { get; set; }
    public int PassedCount { get; set; }
    public int PassRatePercent => TotalAttempts == 0 ? 0 : (int)Math.Round(100.0 * PassedCount / TotalAttempts);

    public List<PositionSummary> AvailablePositions { get; set; } = new();
}

public class ManagerDashboardVm
{
    public List<PositionSummary> MyPositions { get; set; } = new();
    public List<PositionSummary> NeedsAttention { get; set; } = new();
}

public class AdminDashboardVm
{
    public int TotalUsers { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
    public int TotalPositions { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalAttempts { get; set; }

    public List<PositionSummary> AllPositions { get; set; } = new();
}
