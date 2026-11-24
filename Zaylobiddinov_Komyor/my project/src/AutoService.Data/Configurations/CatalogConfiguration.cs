using AutoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoService.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> b)
    {
        b.ToTable("Services");
        b.HasKey(s => s.Id);
        b.Property(s => s.Name).HasMaxLength(150).IsRequired();
        b.Property(s => s.Specialization).HasConversion<string>().HasMaxLength(20);
        b.Ignore(s => s.Price); // цена вычисляется
    }
}

public class SparePartConfiguration : IEntityTypeConfiguration<SparePart>
{
    public void Configure(EntityTypeBuilder<SparePart> b)
    {
        b.ToTable("SpareParts");
        b.HasKey(p => p.Id);
        b.Property(p => p.Article).HasMaxLength(50).IsRequired();
        b.Property(p => p.Name).HasMaxLength(150).IsRequired();
        b.HasIndex(p => p.Article).IsUnique();
    }
}
