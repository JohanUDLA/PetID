using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetID.Data;
using PetID.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace PetID.Controllers
{
    public class HomeController : Controller
    {
        private readonly PetIdDbContext _dbContext;
        private readonly IPasswordHasher<PetUser> _passwordHasher;

        public HomeController(PetIdDbContext dbContext, IPasswordHasher<PetUser> passwordHasher)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var normalizedEmail = email.ToUpperInvariant();
            if (await _dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail))
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe una cuenta con este correo electrónico.");
                return View(model);
            }

            var user = new PetUser
            {
                Email = email,
                NormalizedEmail = normalizedEmail
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            _dbContext.Users.Add(user);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (!await _dbContext.Users.AnyAsync(account => account.NormalizedEmail == normalizedEmail))
                {
                    throw;
                }

                ModelState.AddModelError(nameof(model.Email), "Ya existe una cuenta con este correo electrónico.");
                return View(model);
            }

            TempData["RegistrationSuccess"] = "Tu cuenta fue creada. Ya puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var normalizedEmail = model.Email.Trim().ToUpperInvariant();
                var user = await _dbContext.Users.SingleOrDefaultAsync(
                    account => account.NormalizedEmail == normalizedEmail);

                if (user is not null &&
                    _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) != PasswordVerificationResult.Failed)
                {
                    var claims = new[] { new Claim(ClaimTypes.Name, user.Email) };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                    return RedirectToAction(nameof(Dashboard));
                }

                ModelState.AddModelError("", "Email o contraseña inválidos");
            }

            return View(model);
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

