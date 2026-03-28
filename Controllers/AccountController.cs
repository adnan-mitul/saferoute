using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeRoute.Data;
using SafeRoute.Models;
using SafeRoute.ViewModels;

namespace UsersApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;
        private readonly AppDbContext _context;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, AppDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        // GET: Login
        public IActionResult Login() => View();

        // POST: Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    // Check if admin
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Dashboard", "Admin");
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Email or password is incorrect.");
            return View(model);
        }

        // GET: Register
        public IActionResult Register() => View();

        // POST: Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new Users
            {
                FullName = model.Name,
                Email = model.Email,
                UserName = model.Email,
                JoinedAt = DateTime.UtcNow
            };

            try
            {
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                ModelState.AddModelError("", "An account with this email already exists.");
            }

            return View(model);
        }

        // GET: Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var reports = await _context.IncidentReports
                .Include(r => r.Category)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();

            var contacts = await _context.TrustedContacts
                .Where(c => c.UserId == user.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.TotalReports = reports.Count;
            ViewBag.VerifiedReports = reports.Count(r => r.Status == "Verified");
            ViewBag.PendingReports = reports.Count(r => r.Status == "Pending");
            ViewBag.RatingsGiven = await _context.SafetyRatings.CountAsync(r => r.UserId == user.Id);
            ViewBag.Reports = reports;
            ViewBag.Contacts = contacts;

            return View(user);
        }

        // POST: UpdateProfile
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string fullName, string? phoneNumber, string? address)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            user.FullName = fullName ?? user.FullName;
            user.PhoneNumber = phoneNumber;
            user.Address = address;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // POST: AddContact
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddContact(string name, string phone, string relationship)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var count = await _context.TrustedContacts.CountAsync(c => c.UserId == user.Id);
            if (count >= 5)
            {
                TempData["Error"] = "Maximum 5 trusted contacts allowed.";
                return RedirectToAction("Profile");
            }

            _context.TrustedContacts.Add(new TrustedContact
            {
                UserId = user.Id,
                Name = name,
                Phone = phone,
                Relationship = relationship ?? "Friend"
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Contact added successfully!";
            return RedirectToAction("Profile");
        }

        // POST: DeleteContact
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteContact(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var contact = await _context.TrustedContacts
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
            if (contact != null)
            {
                _context.TrustedContacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Contact removed.";
            return RedirectToAction("Profile");
        }

        // GET: Notifications
        [Authorize]
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(notifications);
        }

        // GET: VerifyEmail
        public IActionResult VerifyEmail() => View();

        // POST: VerifyEmail
        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email not found!");
                return View(model);
            }

            return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
        }

        // GET: ChangePassword
        public IActionResult ChangePassword(string? username)
        {
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("VerifyEmail", "Account");

            return View(new ChangePasswordViewModel { Email = username! });
        }

        // POST: ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Something went wrong. Try again.");
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email not found!");
                return View(model);
            }

            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                foreach (var error in removeResult.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            var addResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addResult.Succeeded)
            {
                foreach (var error in addResult.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            return RedirectToAction("Login", "Account");
        }

        // Logout
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
