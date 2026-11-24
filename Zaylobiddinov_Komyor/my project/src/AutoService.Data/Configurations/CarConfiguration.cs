using AutoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoService.Data.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> b)
    {
        b.ToTable("Cars");
        b.HasKey(c => c.Id);
        b.Property(c => c.Brand).HasMaxLength(50).IsRequired();
        b.Property(c => c.Model).HasMaxLength(50).IsRequired();
        b.Property(c => c.PlateNumber).HasMaxLength(20).IsRequired();
        b.Property(c => c.Vin).HasMaxLength(17);
        b.HasIndex(c => c.PlateNumber).IsUnique();
    }
}
