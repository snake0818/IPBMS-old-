using System.ComponentModel.DataAnnotations.Schema;

namespace PigDB_API.Models
{
    [Table("EstimateRecord")]
    public class EstimateRecord
    {
        public int Id { get; set; }
        public required string DataPath { get; set; }
        public required string ImagePath { get; set; }
        public required string DepthMapPath { get; set; }
        public required long Timestamp { get; set; }

        [ForeignKey("ImageId")]
        public int ImageId { get; set; }
    }
}
