using Microsoft.EntityFrameworkCore;
using PigDB_API.Models;

namespace PigDB_API.Data
{
    public class PigDBContext(DbContextOptions<PigDBContext> options) : DbContext(options)
    {
        public DbSet<Image> Images { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<PigAnnotation> PigAnnotations { get; set; }
        public DbSet<EstimateRecord> EstimateRecords { get; set; }
        public DbSet<TrackingRecord> TrackingRecords { get; set; }
    }
}
