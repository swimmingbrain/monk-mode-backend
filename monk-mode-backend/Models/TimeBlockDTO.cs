using System;
using System.Collections.Generic;
using monk_mode_backend.Domain;
using monk_mode_backend.Models;

namespace monk_mode_backend.Models
{
    /// <summary>
    /// Response-DTO für TimeBlocks (read-only).
    /// Änderungen:
    /// - Als Response-only markiert (enthält Tasks-Liste).
    /// - Für Input Create/UpdateTimeBlockDTO nutzen (ohne Tasks & Systemfelder).
    /// </summary>
    public class TimeBlockDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsFocus { get; set; }

        public List<TaskDTO> Tasks { get; set; } = new();
    }
}
