using System;

namespace monk_mode_backend.Models
{
    public class DailyStatisticsDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime Date { get; set; }
        public int TotalFocusTime { get; set; }
        public string? Username { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
    }
} 