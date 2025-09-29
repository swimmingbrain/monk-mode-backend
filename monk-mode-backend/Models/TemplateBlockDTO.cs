using System;

namespace monk_mode_backend.Models
{
    public class TemplateBlockDTO
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string Title { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsFocus { get; set; }
    }
} 