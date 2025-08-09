namespace ITAdmin.Shared.Models
{
    public class UserLogin
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsLoggedIn { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}