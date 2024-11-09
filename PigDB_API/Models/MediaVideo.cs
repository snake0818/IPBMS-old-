using System.ComponentModel.DataAnnotations.Schema;

namespace PigDB_API.Models
{
    [Table("Video")]
    public class Video
    {
        public int Id { get; set; }
        public required string FilePath { get; set; }
        public required long Timestamp { get; set; }
    }
}
