using System.ComponentModel.DataAnnotations.Schema;

namespace PigDB_API.Models
{
    [Table("TrackingRecord")]
    public class TrackingRecord
    {
        public int Id { get; set; }
        public required string ImagePath { get; set; }
        public required string VideoPath { get; set; }
        public required string DataPath { get; set; }
        public required long Timestamp { get; set; }

        [ForeignKey("VideoId")]
        public int VideoId { get; set; }
    }
}
