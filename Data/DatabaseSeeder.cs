using HRHelper.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Data;

public static class DatabaseSeeder
{
    private const string DefaultPassword = "Parola1!";
    private static readonly string[] RoleNames = { "Admin", "Manager", "Recruiter" };

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        try
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var context = services.GetRequiredService<ApplicationDbContext>();

            await context.Database.MigrateAsync();

            await SeedRolesAsync(roleManager);

            var adminEmail = config["SeedAdmin:Email"];
            var adminPassword = config["SeedAdmin:Password"];

            await UpsertUserAsync(userManager, adminEmail, adminPassword, "Admin", "System Admin");
            var manager = await UpsertUserAsync(userManager, "manager@hr.com", DefaultPassword, "Manager", "Demo Manager");
            await UpsertUserAsync(userManager, "user@hr.com", DefaultPassword, "Recruiter", "Demo Recruiter");

            if (manager is not null)
            {
                await SeedPositionsAndQuestionsAsync(context, manager.Id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database seeding failed: {ex.Message}");
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in RoleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static async Task<ApplicationUser?> UpsertUserAsync(
        UserManager<ApplicationUser> userManager,
        string? email,
        string? password,
        string role,
        string? fullName)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true,
                FullName = fullName,
                CreatedAt = DateTime.UtcNow
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                Console.WriteLine($"Failed to seed user '{email}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                return null;
            }
        }
        else
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, token, password);
            if (!resetResult.Succeeded)
            {
                Console.WriteLine($"Failed to reset password for '{email}': {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                await userManager.UpdateAsync(user);
            }

            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var existingRoles = await userManager.GetRolesAsync(user);
            if (existingRoles.Any())
            {
                await userManager.RemoveFromRolesAsync(user, existingRoles);
            }
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task SeedPositionsAndQuestionsAsync(ApplicationDbContext context, string managerId)
    {
        if (await context.JobPositions.AnyAsync())
        {
            return;
        }

        var positions = BuildSeedPositions(managerId);
        context.JobPositions.AddRange(positions);
        await context.SaveChangesAsync();
    }

    private static List<JobPosition> BuildSeedPositions(string managerId)
    {
        var now = DateTime.UtcNow;

        var dev = new JobPosition
        {
            Title = "Software Developer",
            Department = "IT",
            Description = "Designs, builds, and maintains software applications across the stack.",
            MustHave = "Strong programming fundamentals; experience with at least one modern language; problem-solving mindset.",
            Technologies = "C#, .NET, JavaScript, SQL, Git, REST APIs",
            InterviewGuide = "Pair on a small coding exercise. Ask about a recent project and a tough bug they solved. Discuss debugging and code-review habits.",
            Jargon = "PR, MR, sprint, refactor, technical debt, pair programming",
            CreatedById = managerId,
            CreatedAt = now,
            Questions = new List<Question>
            {
                Q("Which keyword declares a constant in C#?", "var", "const", "static", "readonly", AnswerOption.B, managerId, now),
                Q("What does HTTP stand for?", "HyperText Transfer Protocol", "HighText Transmission Process", "HyperType Tool Protocol", "HostTransfer Type Protocol", AnswerOption.A, managerId, now),
                Q("Which data structure uses LIFO ordering?", "Queue", "Tree", "Stack", "Graph", AnswerOption.C, managerId, now),
                Q("Time complexity of binary search on a sorted array?", "O(n)", "O(log n)", "O(n^2)", "O(1)", AnswerOption.B, managerId, now),
                Q("Which SQL clause filters rows after grouping?", "WHERE", "GROUP BY", "HAVING", "ORDER BY", AnswerOption.C, managerId, now),
                Q("What is an interface in C# used for?", "To define a contract that classes must implement", "To store data", "To execute SQL queries", "To handle exceptions", AnswerOption.A, managerId, now),
            },
            LearningMaterial = "SoftwareDeveloper"
        };

        var devops = new JobPosition
        {
            Title = "DevOps Engineer",
            Department = "IT",
            Description = "Builds and operates the systems that ship and run code reliably.",
            MustHave = "Linux administration; container orchestration; CI/CD pipelines; networking basics.",
            Technologies = "Docker, Kubernetes, Terraform, AWS/Azure, Jenkins, Bash, YAML",
            InterviewGuide = "Walk through a recent incident and how it was mitigated. Ask about IaC, monitoring, and rollback strategy.",
            Jargon = "CI/CD, IaC, blue-green deploy, canary release, SLO, observability",
            CreatedById = managerId,
            CreatedAt = now,
            Questions = new List<Question>
            {
                Q("Which tool is commonly used for container orchestration?", "Docker", "Kubernetes", "Jenkins", "Git", AnswerOption.B, managerId, now),
                Q("What does CI/CD stand for?", "Continuous Integration / Continuous Deployment", "Code Inspection / Code Distribution", "Container Init / Cluster Deploy", "Cloud Infrastructure / Cloud Delivery", AnswerOption.A, managerId, now),
                Q("Which file defines a Docker image?", "docker.yaml", "Dockerfile", "docker.config", "image.json", AnswerOption.B, managerId, now),
                Q("What is Infrastructure as Code (IaC)?", "Manually configuring servers", "Defining infrastructure in machine-readable files", "A type of database", "A monitoring strategy", AnswerOption.B, managerId, now),
                Q("Which CLI tool manages Kubernetes clusters?", "kubectl", "docker-compose", "terraform", "ansible", AnswerOption.A, managerId, now),
                Q("Primary purpose of a load balancer?", "To store backups", "To distribute traffic across multiple servers", "To compile code", "To monitor logs", AnswerOption.B, managerId, now),
            },
            LearningMaterial = "DevOpsEngineer"
        };

        var hr = new JobPosition
        {
            Title = "HR Specialist",
            Department = "HR",
            Description = "Coordinates recruitment, onboarding, and employee relations.",
            MustHave = "Empathy and communication; familiarity with labor regulations; organizational skills.",
            Technologies = "ATS systems, HRIS, Microsoft Office, basic analytics tools",
            InterviewGuide = "Discuss a difficult employee situation they handled. Test GDPR/employment basics. Roleplay a candidate intake call.",
            Jargon = "ATS, onboarding, KPI, performance review, retention, attrition",
            CreatedById = managerId,
            CreatedAt = now,
            Questions = new List<Question>
            {
                Q("What does GDPR stand for?", "General Data Protection Regulation", "Global Data Privacy Rules", "Government Data Policy Restriction", "General Document Privacy Rights", AnswerOption.A, managerId, now),
                Q("Which document outlines terms of employment?", "Resume", "Employment contract", "Cover letter", "Performance review", AnswerOption.B, managerId, now),
                Q("Typical purpose of a probation period?", "To increase salary", "To evaluate fit and performance", "To grant equity", "To assign training", AnswerOption.B, managerId, now),
                Q("Which assessment best fits hiring a developer?", "Touch typing speed", "Coding challenges and technical interviews", "Public speaking", "Sales pitch", AnswerOption.B, managerId, now),
                Q("What is 'onboarding' in HR?", "Boarding a flight", "Integrating a new employee into the company", "Issuing stocks", "Conducting an exit interview", AnswerOption.B, managerId, now),
                Q("Which is an anti-discrimination hiring practice?", "Hiring by referral only", "Equal opportunity hiring", "Hiring only relatives", "Asking about marital status", AnswerOption.B, managerId, now),
            },
            LearningMaterial = "HRSpecialist"

        };

        var frontend = new JobPosition
        {
            Title = "Frontend Developer",
            Department = "IT",
            Description = "Builds modern, accessible user interfaces and connects them to backend APIs.",
            MustHave = "Strong JavaScript/TypeScript; experience with a modern framework (React, Vue, or Angular); CSS/HTML fundamentals.",
            Technologies = "JavaScript, TypeScript, React, HTML5, CSS3, REST/GraphQL, Vite/Webpack",
            InterviewGuide = "Have them debug a small React component live. Ask about state management, accessibility, and performance budgets.",
            Jargon = "SPA, hydration, hooks, virtual DOM, tree-shaking, lighthouse score",
            CreatedById = managerId,
            CreatedAt = now,
            Questions = new List<Question>
            {
                Q("Which JavaScript feature was introduced in ES6 (2015)?", "var keyword", "arrow functions", "prototype chain", "window object", AnswerOption.B, managerId, now),
                Q("What does the React useState hook return?", "A class instance", "An array with the state value and a setter function", "A promise", "A DOM node", AnswerOption.B, managerId, now),
                Q("Which CSS rule creates a flex container?", "display: block", "display: flex", "position: absolute", "float: left", AnswerOption.B, managerId, now),
                Q("What is the main benefit of the Virtual DOM?", "Storing user data", "Minimizing actual DOM updates for better rendering performance", "Replacing JavaScript", "Handling network requests", AnswerOption.B, managerId, now),
                Q("Which HTTP method is typically used to retrieve data?", "POST", "PUT", "GET", "DELETE", AnswerOption.C, managerId, now),
                Q("What is the role of a bundler like Webpack or Vite?", "To run the database", "To package assets and modules for the browser", "To monitor server uptime", "To compile native code", AnswerOption.B, managerId, now),
            },
            LearningMaterial = "FrontendDeveloper"
        };

        var qa = new JobPosition
        {
            Title = "QA Engineer",
            Department = "IT",
            Description = "Designs and runs automated and manual tests to safeguard product quality.",
            MustHave = "Strong attention to detail; experience writing test cases; familiarity with at least one automation framework.",
            Technologies = "Selenium, Playwright, xUnit/NUnit, Postman, JIRA, SQL",
            InterviewGuide = "Ask them to design test cases for a small feature. Discuss flaky tests and how to debug them. Walk through a recent bug they caught.",
            Jargon = "regression, smoke test, fixture, flaky, test pyramid, coverage",
            CreatedById = managerId,
            CreatedAt = now,
            Questions = new List<Question>
            {
                Q("What's the difference between unit and integration tests?", "Unit tests verify individual components in isolation; integration tests verify how components work together", "Unit tests run in production; integration tests run locally", "There is no difference", "Unit tests are written by QA; integration tests by developers", AnswerOption.A, managerId, now),
                Q("Which testing framework is commonly used for .NET?", "JUnit", "xUnit", "PyTest", "Mocha", AnswerOption.B, managerId, now),
                Q("What does 'regression testing' mean?", "Testing only new features", "Re-testing existing features after changes to ensure nothing broke", "Testing the database", "Reverting code", AnswerOption.B, managerId, now),
                Q("What is the purpose of a test plan?", "To document what to test, how, and when", "To track bugs", "To deploy code", "To monitor uptime", AnswerOption.A, managerId, now),
                Q("What is a 'flaky test'?", "A test that always passes", "A test with inconsistent results across runs", "A type of UI test", "A failing build", AnswerOption.B, managerId, now),
                Q("Which technique gives an application unexpected inputs to find bugs?", "Smoke testing", "Fuzz testing", "Load testing", "Linting", AnswerOption.B, managerId, now),
            },
            LearningMaterial = "QAEngineer"
        };

        var recruiter = new JobPosition
        {
            Title = "Recruiter",
            Department = "HR",
            Description = "Sources, screens, and shepherds candidates through the hiring pipeline.",
            MustHave = "Excellent communication; experience with at least one ATS; comfortable evaluating CVs and conducting screening calls.",
            Technologies = "LinkedIn Recruiter, Greenhouse/Lever, Outlook, Calendly, Excel",
            InterviewGuide = "Roleplay an inbound candidate intake call. Ask about their best sourcing channels and how they handle a tough close.",
            Jargon = "ATS, pipeline, sourcing, time-to-hire, offer rate, passive candidate",
            CreatedById = managerId,
            CreatedAt = now,
            Questions = new List<Question>
            {
                Q("What is a 'sourcing channel'?", "A network protocol", "A method or platform used to find candidates", "A type of contract", "A reporting tool", AnswerOption.B, managerId, now),
                Q("What is typically the first step in a recruitment pipeline?", "Job offer", "Sourcing candidates", "Onboarding", "Salary negotiation", AnswerOption.B, managerId, now),
                Q("Which question is illegal to ask during a job interview in many jurisdictions?", "Marital status", "Years of professional experience", "Skills relevant to the role", "Availability to start", AnswerOption.A, managerId, now),
                Q("What does 'time-to-hire' measure?", "The interview duration", "The days from job posting to candidate accepting an offer", "The time spent training new hires", "The offer validity period", AnswerOption.B, managerId, now),
                Q("What is a 'passive candidate'?", "Someone unemployed", "A candidate not actively looking but open to opportunities", "A person rejected for a role", "A candidate who skips interviews", AnswerOption.B, managerId, now),
                Q("Which metric tracks how many candidates progress from one stage to the next?", "Funnel conversion rate", "Bounce rate", "ROI", "NPS", AnswerOption.A, managerId, now),
            },
             LearningMaterial = "Recruiter"

        };

        return new List<JobPosition> { dev, devops, frontend, qa, hr, recruiter };
    }

    private static Question Q(string text, string a, string b, string c, string d, AnswerOption correct, string createdBy, DateTime createdAt)
    {
        return new Question
        {
            Text = text,
            OptionA = a,
            OptionB = b,
            OptionC = c,
            OptionD = d,
            CorrectAnswer = correct,
            CreatedById = createdBy,
            CreatedAt = createdAt
        };
    }
}
