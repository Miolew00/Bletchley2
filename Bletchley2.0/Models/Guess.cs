namespace Bletchley2._0.Models
{
    public class Guess
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public Game? Game { get; set; }
        public string GuessCode { get; set; } = string.Empty;
        public int KnownNumbers { get; set; }
        public int KnownPositions { get; set; }
        public int AttemptNumber { get; set; } // This will help us track the order of guesses for each game
    }
}