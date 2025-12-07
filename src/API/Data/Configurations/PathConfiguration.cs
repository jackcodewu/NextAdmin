using NextAdmin.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NextAdmin.API.Data.Configurations
{
    public class SchedulingTaskLineConfiguration : IEntityTypeConfiguration<SchedulingTaskLine>
    {
        public void Configure(EntityTypeBuilder<SchedulingTaskLine> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedOnAdd();
            builder.Property(p => p.VehicleId).IsRequired();
            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Description).HasMaxLength(500);
            builder.Property(p => p.Status).IsRequired();
            builder.HasMany(p => p.PathPoints)
                .WithOne()
                .HasForeignKey(pp => pp.SchedulingTaskLineId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SchedulingTaskLinePointConfiguration : IEntityTypeConfiguration<PathPoint>
    {
        public void Configure(EntityTypeBuilder<PathPoint> builder)
        {
            builder.HasKey(pp => pp.Id);
            builder.Property(pp => pp.Id).ValueGeneratedOnAdd();
            builder.Property(pp => pp.SchedulingTaskLineId).IsRequired();
            builder.Property(pp => pp.SequenceNumber).IsRequired();
            builder.Property(pp => pp.Type).IsRequired();
            builder.OwnsOne(pp => pp.Position);
            builder.Property(pp => pp.Heading).IsRequired(false);
            builder.Property(pp => pp.Speed).IsRequired(false);
        }
    }
} 
