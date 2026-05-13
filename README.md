# HRHelper

ASP.NET Core MVC application that helps HR managers prepare interview kits and lets recruiters validate their understanding of a role through short, randomized quizzes.

A manager defines a job position (description, must-haves, technologies, interview guide, jargon) and writes a question bank for it. Once a position has at least 5 questions, recruiters can take a 3-question randomized quiz to confirm they're prepared to screen candidates for that role.

---

## Tech stack

- ASP.NET Core MVC on **.NET 10**
- ASP.NET Core Identity (cookie auth, role-based authorization)
- Entity Framework Core 10 with **SQL Server LocalDB**
- Razor views + Bootstrap (default ASP.NET Core template)

---

## Getting started

### Prerequisites
- .NET 10 SDK
- SQL Server LocalDB (ships with Visual Studio; otherwise install via the SQL Server Express installer)
- `dotnet-ef` global tool (for database commands): `dotnet tool install --global dotnet-ef`

### Run
```powershell
dotnet run
```
Migrations apply automatically on startup, and the seeder creates roles, demo users, and 6 demo job positions with their question banks.

### Default credentials (dev only)
All seeded users share the same password: **`Parola1!`**

| Email           | Role      |
|-----------------|-----------|
| admin@hr.com    | Admin     |
| manager@hr.com  | Manager   |
| user@hr.com     | Recruiter |

The admin email/password come from [appsettings.Development.json](appsettings.Development.json); the other two are hard-coded in [Data/DatabaseSeeder.cs](Data/DatabaseSeeder.cs).

---

## Roles & authorization

Three roles are seeded at startup: **Admin**, **Manager**, **Recruiter**. Each user has exactly one role.

| Area / action                                  | Admin | Manager | Recruiter |
|------------------------------------------------|:-----:|:-------:|:---------:|
| Register (self-service, gets Recruiter role)   |  -    |    -    |    yes    |
| View job positions list & details              |  yes  |   yes   |    yes    |
| Create / edit / delete job positions           |  yes  |   yes   |    -      |
| Manage questions for a position                |  yes  |   yes   |    -      |
| Take a quiz / view own attempts                |  yes  |    -    |    yes    |
| Activity feed (completed attempts)             |  all  | own positions only | - |
| User management (`/Admin/Users`)               |  yes  |    -    |    -      |

Authorization is enforced at controller level via `[Authorize(Roles = "...")]` ([AdminController.cs:15](Controllers/AdminController.cs#L15), [JobPositionsController.cs](Controllers/JobPositionsController.cs), [QuestionsController.cs:13](Controllers/QuestionsController.cs#L13), [QuizController.cs:15](Controllers/QuizController.cs#L15), [ActivityController.cs:12](Controllers/ActivityController.cs#L12)). New self-registrations are auto-assigned the `Recruiter` role ([Register.cshtml.cs:127](Areas/Identity/Pages/Account/Register.cshtml.cs#L127)). Deactivated users (`IsActive = false`) are signed out at login ([Login.cshtml.cs:121-126](Areas/Identity/Pages/Account/Login.cshtml.cs#L121-L126)).

---

## Functionality

### Authentication ([Areas/Identity/](Areas/Identity/))
- Email + password login (Identity scaffolded UI)
- Self-service registration → new user becomes a **Recruiter**
- Soft-deactivation: an inactive user can't sign in, even with valid credentials

### Home dashboards ([HomeController.cs](Controllers/HomeController.cs))
The landing page renders one of three dashboards based on role:
- **Admin** ([_AdminDashboard.cshtml](Views/Home/_AdminDashboard.cshtml)) — totals (users, positions, questions, completed attempts), users-by-role breakdown, all positions overview
- **Manager** ([_ManagerDashboard.cshtml](Views/Home/_ManagerDashboard.cshtml)) — positions they own, with question count and attempt count; positions with fewer than 5 questions are flagged as "needs attention"
- **Recruiter** ([_RecruiterDashboard.cshtml](Views/Home/_RecruiterDashboard.cshtml)) — completed attempts, pass rate, and the list of positions ready to quiz on

### Job positions ([JobPositionsController.cs](Controllers/JobPositionsController.cs))
- List with search-by-title and department filter ([Index.cshtml](Views/JobPositions/Index.cshtml))
- Details view shows full job description, must-have skills, technologies, interview guide, jargon ([Details.cshtml](Views/JobPositions/Details.cshtml))
- Admin/Manager: create, edit, delete
- Each position tracks `CreatedBy` / `UpdatedBy` (id + email) and timestamps

### Question bank ([QuestionsController.cs](Controllers/QuestionsController.cs))
- Multiple-choice questions (4 options A/B/C/D, one correct answer) scoped to a job position
- Routes are nested under the position: `/JobPositions/{id}/Questions`, `/JobPositions/{id}/Questions/Create`
- Admin/Manager only; questions track `CreatedBy` / `UpdatedBy` and timestamps
- A position needs **≥ 5 questions** before a quiz can be taken

### Quiz ([QuizController.cs](Controllers/QuizController.cs))
- Recruiter (or admin for testing) starts a quiz for a position via `/Quiz/Start/{jobPositionId}`
- 3 questions are randomly drawn from the bank ([QuizController.cs:18](Controllers/QuizController.cs#L18))
- Answer options are also shuffled per question ([QuizController.cs:218](Controllers/QuizController.cs#L218))
- "Pass" requires **3/3 correct** ([QuizController.cs:145](Controllers/QuizController.cs#L145))
- Selected questions and given answers (correct/incorrect) are persisted for review
- `/Quiz/MyAttempts` lists the recruiter's own history; `/Quiz/Result/{attemptId}` shows the per-question breakdown

### Activity feed ([ActivityController.cs](Controllers/ActivityController.cs))
- `/Activity` lists the 100 most recent **completed** quiz attempts
- Admin sees all attempts; Manager sees only attempts on positions they created
- Each row shows position, recruiter email, score, pass/fail, timestamps

### Admin: user management ([AdminController.cs](Controllers/AdminController.cs))
- `/Admin/Users` — paginated user list (20/page) with search by email/name, filter by role, filter by active/inactive
- Create user — admin sets email + role; system generates a 12-char temp password and shows it once (`TempData`)
- Edit user — change full name, role, active flag (with self-protection: can't change own role or deactivate self)
- Reset password — generates and shows a new temp password
- Delete — soft-delete: sets `IsActive = false`; the user is preserved for audit history but blocked at login

---

## Domain model ([Models/](Models/))

```
ApplicationUser  : ApplicationUser   FullName, IsActive, CreatedAt
                                  └─ roles: Admin | Manager | Recruiter
JobPosition                       Title, Department, Description, MustHave,
                                  Technologies, InterviewGuide, Jargon,
                                  CreatedBy/UpdatedBy + timestamps
   └─ Questions (1..*)            Text, OptionA-D, CorrectAnswer (A|B|C|D),
                                  CreatedBy/UpdatedBy + timestamps
QuizAttempt                       RecruiterId, JobPositionId, Started/CompletedAt,
                                  Score (0-3), Passed (Score == 3)
   ├─ SelectedQuestions (1..3)    QuestionId + DisplayOrder (frozen at start)
   └─ Answers       (0..3)        QuestionId, SelectedOption, IsCorrect
```

---

## Project structure

```
HRHelper/
├── Areas/Identity/        Scaffolded Identity Razor pages (login, register, ...)
├── Controllers/           MVC controllers (Home, Admin, JobPositions, Questions, Quiz, Activity)
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DatabaseSeeder.cs        Seeds roles, demo users, and 6 job positions
│   └── Migrations/              EF Core migrations
├── Models/                Domain entities + view models
├── Views/                 Razor views (one folder per controller + Shared)
├── wwwroot/               Static assets (Bootstrap, jQuery, app.css)
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

---

## Database

### Connection
Configured in [appsettings.json](appsettings.json):
```
Server=(localdb)\mssqllocaldb;Database=HRHelper;Trusted_Connection=True;MultipleActiveResultSets=true
```

### Migrations
Migrations live in [Data/Migrations/](Data/Migrations/). They apply automatically on startup ([DatabaseSeeder.cs:20](Data/DatabaseSeeder.cs#L20)). To work with them manually:
```powershell
dotnet ef migrations add <Name>
dotnet ef database update
```

### Seeding
On every startup the seeder ([Data/DatabaseSeeder.cs](Data/DatabaseSeeder.cs)) will:
- Create the three roles if missing
- **Upsert** the three demo users (resets password, ensures role, clears lockout, marks active)
- Seed the 6 demo job positions with their questions **only if `JobPositions` is empty**

### Resetting the database
The seeder will not re-create job positions while any exist. To get a clean reseed:
```powershell
dotnet ef database drop -f
dotnet run
```

---

## Configuration

| Setting                          | Where                                   | Notes                                  |
|----------------------------------|-----------------------------------------|----------------------------------------|
| `ConnectionStrings:DefaultConnection` | `appsettings.json`                | LocalDB by default                     |
| `SeedAdmin:Email` / `:Password`  | `appsettings.Development.json`          | Move to user-secrets before deploying  |

> **Note:** `appsettings.Development.json` currently contains the admin password in plaintext. Fine for the demo, but for any non-local use move it to `dotnet user-secrets` or environment variables.

---

## Constants worth knowing

| Constant                   | Value | Defined in                                                    |
|----------------------------|------:|---------------------------------------------------------------|
| Min questions for a quiz   | 5     | [HomeController.cs:14](Controllers/HomeController.cs#L14), [QuestionsController.cs:16](Controllers/QuestionsController.cs#L16), [QuizController.cs:19](Controllers/QuizController.cs#L19) |
| Questions per attempt      | 3     | [QuizController.cs:18](Controllers/QuizController.cs#L18)     |
| Pass threshold             | 3 / 3 | [QuizController.cs:145](Controllers/QuizController.cs#L145)   |
| Activity feed page size    | 100   | [ActivityController.cs:15](Controllers/ActivityController.cs#L15) |
| Admin user list page size  | 20    | [AdminController.cs:18](Controllers/AdminController.cs#L18)   |
