namespace DocManagerAI.Models
{
    public class User
    {
        public int    Id           { get; set; }
        public string Username     { get; set; }
        public string PasswordHash { get; set; }
        public string Role         { get; set; }  // Admin | Reviewer | Manager | Finance | Viewer
        public string Email        { get; set; }
        public DateTime CreatedAt  { get; set; } = DateTime.Now;
    }
}
