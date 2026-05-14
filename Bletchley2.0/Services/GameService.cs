namespace Bletchley2._0.Services
{
    public class GameService
    {
        public string GenerateSecretCode()
        {
            var pool = Enumerable.Range(0, 8).OrderBy(_ => Guid.NewGuid()).Take(4);
            return string.Join(" ", pool);
        }

        public (int knownNumbers, int knownPositions) CheckGuess(string secret, string guess)
        {
            var s = secret.Split(' ').Select(int.Parse).ToList();
            var g = guess.Split(' ').Select(int.Parse).ToList();

            int knownPositions = s.Zip(g, (a, b) => a == b).Count(x => x);
            int knownNumbers = s.Intersect(g).Count();

            return (knownNumbers, knownPositions);
        }

        public int CalculateScore(int attempts)
        {
            if (attempts <= 3) return 20;
            if (attempts <= 7) return 10;
            if (attempts <= 10) return 5;
            return 3;
        }
    }
}