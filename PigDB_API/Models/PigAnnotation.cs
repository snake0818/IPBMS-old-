using System.ComponentModel.DataAnnotations.Schema;

namespace PigDB_API.Models
{
    [Table("PigAnnotation")]
    public class PigAnnotation
    {
        public int Id { get; set; }
        public required string ImagePath { get; set; }
        public required string DataPath { get; set; }

        [ForeignKey("RecordId")]
        public int RecordId { get; set; }
    }
}
