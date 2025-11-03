using System;
using System.Collections.Generic;

namespace monk_mode_backend.Models
{
    public class TemplateDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TemplateBlockDTO> TemplateBlocks { get; set; }
    }
} 