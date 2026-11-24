using AutoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoService.Data.Configurations;

/// <summary>
/// Иерархия Person → Client / Employee → Mechanic / Receptionist хранится в одной таблице (TPH).
/// Тип объекта определяется по столбцу Discriminator.
/// </summary>
public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable("Persons");
        b.HasKey(p => p.Id);
        b.Property(p => p.FullName).HasMaxLength(150).IsRequired();
        b.Property(p => p.Phone).HasMaxLength(30).IsRequired();

        b.HasDiscriminator<string>("Discriminator")
            .HasValue<Client>("Client")
            .HasValue<Mechanic>("Mechanic")
            .HasValue<Receptionist>("Receptionist");
    }
}

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.Property(c => c.Email).HasMaxLength(100);

        b.HasMany(c => c.Cars)
            .WithOne(car => car.Client)
            .HasForeignKey(car => car.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // коллекция хранится в закрытом поле _cars — EF работает с полем, не нарушая инкапсуляцию
        b.Navigation(c => c.Cars).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.Ignore(e => e.Position); // вычисляемое свойство
    }
}

public class MechanicConfiguration : IEntityTypeConfiguration<Mechanic>
{
    public void Configure(EntityTypeBuilder<Mechanic> b)
    {
        b.Property(m => m.Specialization).HasConversion<string>().HasMaxLength(20);
    }
}
