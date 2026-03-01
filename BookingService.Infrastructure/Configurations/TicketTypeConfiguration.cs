using BookingService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Configurations;

public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("TicketTypes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.Capacity)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired();

        builder.Property(t => t.ReservedQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.SoldQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.RowVersion)
            .IsRowVersion();

        builder.HasOne(t => t.Event)
            .WithMany(e => e.TicketTypes)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.EventId);
        builder.HasIndex(t => new { t.EventId, t.IsActive });
        builder.HasIndex(t => new { t.EventId, t.Name })
            .IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_TicketTypes_Capacity",
                "Capacity >= 0");

            t.HasCheckConstraint(
                "CK_TicketTypes_Reserved",
                "ReservedQuantity >= 0");

            t.HasCheckConstraint(
                "CK_TicketTypes_Sold",
                "SoldQuantity >= 0");

            t.HasCheckConstraint(
                "CK_TicketTypes_Total",
                "ReservedQuantity + SoldQuantity <= Capacity");
        });
    }
}

