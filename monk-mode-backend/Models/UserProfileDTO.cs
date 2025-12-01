namespace monk_mode_backend.Models
{
    public class UserProfileDTO
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public int Xp {  get; set; }
        public int Level { get; set; }
    }
}
