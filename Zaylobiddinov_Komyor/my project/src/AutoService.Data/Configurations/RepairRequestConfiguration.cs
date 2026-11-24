using AutoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoService.Data.Configurations;

public class RepairRequestConfiguration : IEntityTypeConfiguration<RepairRequest>
{
    public void Configure(EntityTypeBuilder<RepairRequest> b)
    {
        b.ToTable("RepairRequests");
        b.HasKey(r => r.Id);
        b.Property(r => r.Number).HasMaxLength(20).IsRequired();
        b.HasIndex(r => r.Number).IsUnique();
        b.Property(r => r.Problem).HasMaxLength(1000).IsRequired();
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(30);

        b.HasOne(r => r.Client).WithMany().HasForeignKey(r => r.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Car).WithMany().HasForeignKey(r => r.CarId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Receptionist).WithMany().HasForeignKey(r => r.ReceptionistId).OnDelete(DeleteBehavior.Restrict);

        // композиция: позиции заявки удаляются вместе с ней
        b.HasMany(r => r.Works).WithOne().HasForeignKey(w => w.RepairRequestId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(r => r.Parts).WithOne().HasForeignKey(p => p.RepairRequestId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(r => r.Works).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(r => r.Parts).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasOne(r => r.Payment).WithOne()
            .HasForeignKey<Payment>(p => p.RepairRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // вычисляемые свойства в базе не хранятся
        b.Ignore(r => r.WorksSum);
        b.Ignore(r => r.PartsSum);
        b.Ignore(r => r.Subtotal);
        b.Ignore(r => r.DiscountAmount);
        b.Ignore(r => r.Total);
        b.Ignore(r => r.IsEditable);
        b.Ignore(r => r.IsClosed);
    }
}

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> b)
    {
        b.ToTable("WorkItems");
        b.HasKey(w => w.Id);
        b.HasOne(w => w.Service).WithMany().HasForeignKey(w => w.ServiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(w => w.Mechanic).WithMany().HasForeignKey(w => w.MechanicId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PartItemConfiguration : IEntityTypeConfiguration<PartItem>
{
    public void Configure(EntityTypeBuilder<PartItem> b)
    {
        b.ToTable("PartItems");
        b.HasKey(p => p.Id);
        b.HasOne(p => p.SparePart).WithMany().HasForeignKey(p => p.SparePartId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(p => p.Sum);
    }
}
