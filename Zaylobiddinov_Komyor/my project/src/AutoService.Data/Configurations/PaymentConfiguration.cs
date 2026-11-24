using AutoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoService.Data.Configurations;

/// <summary>Иерархия Payment → CashPayment / CardPayment в одной таблице (TPH).</summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.HasKey(p => p.Id);
        b.Ignore(p => p.Method);
        b.HasDiscriminator<string>("Discriminator")
            .HasValue<CashPayment>("Cash")
            .HasValue<CardPayment>("Card");
    }
}

public class CashPaymentConfiguration : IEntityTypeConfiguration<CashPayment>
{
    public void Configure(EntityTypeBuilder<CashPayment> b) => b.Ignore(p => p.Change);
}

public class CardPaymentConfiguration : IEntityTypeConfiguration<CardPayment>
{
    public void Configure(EntityTypeBuilder<CardPayment> b) => b.Property(p => p.CardLast4).HasMaxLength(4);
}
