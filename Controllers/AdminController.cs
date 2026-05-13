using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRHelper.Data;
using HRHelper.Models;

namespace HRHelper.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const int PageSize = 20;
        private static readonly string[] AvailableRoles = { "Admin", "Manager", "Recruiter" };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Users(string? q, string? role, string status = "all", int page = 1)
        {
            if (page < 1) page = 1;

            IQueryable<ApplicationUser> query = _userManager.Users;

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLower = q.Trim().ToLower();
                query = query.Where(u => (u.Email ?? "").ToLower().Contains(qLower)
                                      || (u.FullName ?? "").ToLower().Contains(qLower));
            }

            if (status == "active")
            {
                query = query.Where(u => u.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(u => !u.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(role) && AvailableRoles.Contains(role))
            {
                var roleEntity = await _roleManager.FindByNameAsync(role);
                if (roleEntity is not null)
                {
                    var userIdsInRole = _context.UserRoles
                        .Where(ur => ur.RoleId == roleEntity.Id)
                        .Select(ur => ur.UserId);
                    query = query.Where(u => userIdsInRole.Contains(u.Id));
                }
            }

            var totalCount = await query.CountAsync();

            var pageUsers = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var pageIds = pageUsers.Select(u => u.Id).ToList();
            var roleByUserId = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where pageIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name })
                .ToListAsync();

            var roleMap = roleByUserId
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.First().RoleName);

            var currentUserId = _userManager.GetUserId(User);
            var items = pageUsers.Select(u => new UserListItem
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName,
                Role = roleMap.GetValueOrDefault(u.Id),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                IsSelf = u.Id == currentUserId
            }).ToList();

            var vm = new UserListViewModel
            {
                Users = items,
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount,
                Q = q,
                Role = role,
                Status = status,
                AllRoles = AvailableRoles.ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel { AllRoles = AvailableRoles.ToList() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            model.AllRoles = AvailableRoles.ToList();

            if (!AvailableRoles.Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Invalid role.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing is not null)
            {
                ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
                return View(model);
            }

            var tempPassword = GenerateTempPassword();
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = string.IsNullOrWhiteSpace(model.FullName) ? null : model.FullName.Trim(),
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            TempData["TempPassword"] = tempPassword;
            TempData["TempPasswordEmail"] = model.Email;
            TempData["TempPasswordReason"] = "User created";

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? string.Empty;
            var currentUserId = _userManager.GetUserId(User);

            return View(new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = currentRole,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                IsSelf = user.Id == currentUserId,
                AllRoles = AvailableRoles.ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, EditUserViewModel model)
        {
            if (id != model.Id) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            model.AllRoles = AvailableRoles.ToList();
            model.Email = user.Email ?? string.Empty;
            model.CreatedAt = user.CreatedAt;
            var currentUserId = _userManager.GetUserId(User);
            model.IsSelf = user.Id == currentUserId;

            if (!AvailableRoles.Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Invalid role.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault();

            if (model.IsSelf && currentRole != model.Role)
            {
                ModelState.AddModelError(nameof(model.Role), "You cannot change your own role.");
                model.Role = currentRole ?? string.Empty;
                return View(model);
            }

            if (model.IsSelf && !model.IsActive)
            {
                ModelState.AddModelError(nameof(model.IsActive), "You cannot deactivate your own account.");
                model.IsActive = true;
                return View(model);
            }

            user.FullName = string.IsNullOrWhiteSpace(model.FullName) ? null : model.FullName.Trim();
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            if (currentRole != model.Role)
            {
                if (!string.IsNullOrEmpty(currentRole))
                {
                    await _userManager.RemoveFromRoleAsync(user, currentRole);
                }
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            TempData["StatusMessage"] = "User updated.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var tempPassword = GenerateTempPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);
            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "Failed to reset password: " +
                    string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(EditUser), new { id });
            }

            TempData["TempPassword"] = tempPassword;
            TempData["TempPasswordEmail"] = user.Email;
            TempData["TempPasswordReason"] = "Password reset";

            return RedirectToAction(nameof(EditUser), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["StatusMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            TempData["StatusMessage"] = $"User '{user.Email}' has been deactivated.";
            return RedirectToAction(nameof(Users));
        }

        private static string GenerateTempPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%&*?";
            string all = upper + lower + digits + special;

            var chars = new List<char>
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                special[RandomNumberGenerator.GetInt32(special.Length)]
            };
            for (int i = 0; i < 8; i++)
            {
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
            }

            for (int i = chars.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }
    }
}
