using System;

namespace VibroMonitor.Models
{
    public class EquipmentImage
    {
        public int Id { get; set; }
        public int EquipmentItemId { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string MimeType { get; set; } = "image/png";
        public string FileName { get; set; } = "";
        public bool IsThumbnail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // navigation
        public EquipmentItem? EquipmentItem { get; set; }
    }
}
