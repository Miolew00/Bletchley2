using Bletchley2._0.Data;
using Bletchley2._0.Models;
using Bletchley2._0.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bletchley2._0.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GameService _gameService;

        private const int HintCost = 30;

        public GamesController(ApplicationDbContext db,
                               UserManager<IdentityUser> userManager,
                               GameService gameService)
        {
            _db = db;
            _userManager = userManager;
            _gameService = gameService;
        }

        private async Task<UserProfile> GetOrCreateProfileAsync()
        {
            string userId = _userManager.GetUserId(User) ?? "";

            var profile = await _db.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = userId,
                    ProfilePicturePath = "/images/default-avatar.png",
                    TotalPoints = 0
                };

                _db.UserProfiles.Add(profile);
                await _db.SaveChangesAsync();
            }

            return profile;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Start()
        {
            var userId = _userManager.GetUserId(User);

            var game = new Game
            {
                UserId = userId!,
                SecretCode = _gameService.GenerateSecretCode(),
                PlayedAt = DateTime.Now
            };

            _db.Games.Add(game);
            await _db.SaveChangesAsync();

            return RedirectToAction("Play", new { id = game.Id });
        }

        public async Task<IActionResult> Play(int id)
        {
            var game = await _db.Games
                .Include(g => g.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (game.UserId != userId)
            {
                return Forbid();
            }

            var guesses = await _db.Guesses
                .Where(g => g.GameId == id)
                .OrderBy(g => g.AttemptNumber)
                .ToListAsync();

            var profile = await GetOrCreateProfileAsync();

            ViewBag.Guesses = guesses;

            // Това е важно за hint бутона
            ViewBag.TotalPoints = profile.TotalPoints;
            ViewBag.CanUseHint = profile.TotalPoints >= HintCost;
            ViewBag.HintCost = HintCost;

            return View(game);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitGuess(int gameId, string guessInput)
        {
            var game = await _db.Games.FindAsync(gameId);

            if (game == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (game.UserId != userId)
            {
                return Forbid();
            }

            if (game.IsCompleted)
            {
                return RedirectToAction("Play", new { id = gameId });
            }

            var cleaned = (guessInput ?? "")
                .Trim()
                .Replace(",", " ")
                .Replace("-", " ")
                .Replace(".", " ");

            var parts = cleaned.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            bool valid = parts.Length == 4
                && parts.All(p => int.TryParse(p, out int n) && n >= 0 && n <= 7)
                && parts.Distinct().Count() == 4;

            if (!valid)
            {
                TempData["Error"] = "Въведи 4 уникални числа от 0 до 7! Пример: 3 1 5 7";
                return RedirectToAction("Play", new { id = gameId });
            }

            game.Attempts++;

            var (knownNums, knownPos) = _gameService.CheckGuess(
                game.SecretCode, string.Join(" ", parts));

            _db.Guesses.Add(new Guess
            {
                GameId = gameId,
                GuessCode = string.Join(" ", parts),
                KnownNumbers = knownNums,
                KnownPositions = knownPos,
                AttemptNumber = game.Attempts
            });

            if (knownPos == 4)
            {
                game.IsCompleted = true;
                game.IsWon = true;
                game.Score = _gameService.CalculateScore(game.Attempts);

                // ТОВА ТИ ЛИПСВАШЕ
                var profile = await GetOrCreateProfileAsync();
                profile.TotalPoints += game.Score;

                TempData["Success"] = $"Браво! Победи и спечели {game.Score} точки!";
            }
            else if (game.Attempts >= 13)
            {
                game.IsCompleted = true;
                game.IsWon = false;
                game.Score = 0;

                TempData["Error"] = $"Загуби! Тайният код беше: {game.SecretCode}";
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Play", new { id = gameId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseHint(int id)
        {
            var game = await _db.Games.FindAsync(id);

            if (game == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (game.UserId != userId)
            {
                return Forbid();
            }

            if (game.IsCompleted)
            {
                TempData["Error"] = "Играта вече е приключила.";
                return RedirectToAction("Play", new { id = game.Id });
            }

            var profile = await GetOrCreateProfileAsync();

            if (profile.TotalPoints < HintCost)
            {
                TempData["Error"] = "Нямаш достатъчно точки за hint.";
                return RedirectToAction("Play", new { id = game.Id });
            }

            profile.TotalPoints -= HintCost;

            string[] codeParts = game.SecretCode.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (codeParts.Length >= 4)
            {
                TempData["Hint"] = $"Hint: първата цифра е {codeParts[0]}";
            }
            else
            {
                TempData["Hint"] = $"Hint: първата цифра е {game.SecretCode[0]}";
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Play", new { id = game.Id });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Ranking()
        {
            var results = await _db.Games
                .Include(g => g.User)
                .Where(g => g.IsCompleted)
                .OrderByDescending(g => g.Score)
                .ThenBy(g => g.Attempts)
                .ThenBy(g => g.PlayedAt)
                .ToListAsync();

            return View(results);
        }
    }
}