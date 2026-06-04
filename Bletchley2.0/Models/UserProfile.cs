namespace Bletchley2._0.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ProfilePicturePath { get; set; } = "/images/default-avatar.png";
        public int TotalPoints { get; set; } = 0;
    }
}