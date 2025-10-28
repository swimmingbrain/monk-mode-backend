using System;

namespace monk_mode_backend.Models
{
    /// summary
    /// Response-DTO für Tagesstatistiken (read-only).
    /// Änderungen:
    /// - Klar als Response-only markiert (nicht als Input akzeptieren).

    public class DailyStatisticsDTO
    {
        public string UserId { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public int TotalFocusTime { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
    }
}
