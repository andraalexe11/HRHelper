using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRHelper.Models;

public class UserListViewModel
{
    public List<UserListItem> Users { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => TotalCount == 0 ? 1 : (TotalCount + PageSize - 1) / PageSize;

    public string? Q { get; set; }
    public string? Role { get; set; }
    public string Status { get; set; } = "all";

    public List<string> AllRoles { get; set; } = new();
}

public class UserListItem
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsSelf { get; set; }
}

public class CreateUserViewModel
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(150)]
    public string? FullName { get; set; }

    [Required]
    public string Role { get; set; } = "Recruiter";

    public List<string> AllRoles { get; set; } = new();
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(150)]
    public string? FullName { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsSelf { get; set; }
    public List<string> AllRoles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
