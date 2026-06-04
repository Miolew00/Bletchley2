using Bletchley2._0.Data;
using Bletchley2._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bletchley2._0.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new UserProfile { UserId = userId! };
                _db.UserProfiles.Add(profile);
                await _db.SaveChangesAsync();
            }

            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> UploadPicture(IFormFile profilePicture)
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new UserProfile { UserId = userId! };
                _db.UserProfiles.Add(profile);
            }

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(profilePicture.FileName).ToLower();

                if (!allowed.Contains(ext))
                {
                    TempData["Error"] = "Само снимки са позволени!";
                    return RedirectToAction("Index");
                }

                if (profilePicture.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "Снимката трябва да е под 2MB!";
                    return RedirectToAction("Index");
                }

                var fileName = $"{userId}{ext}";
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                Directory.CreateDirectory(folder);
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                profile.ProfilePicturePath = $"/images/profiles/{fileName}";
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Снимката е обновена успешно!";
            return RedirectToAction("Index");
        }
    }
}