using Microsoft.AspNetCore.Identity;

namespace Bletchley2._0.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public IdentityUser? User { get; set; }
        public string SecretCode { get; set; } = string.Empty;
        public int Attempts { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public bool IsWon { get; set; } = false;
        public int Score { get; set; } = 0;
        public DateTime PlayedAt { get; set; } = DateTime.Now;
    }
}