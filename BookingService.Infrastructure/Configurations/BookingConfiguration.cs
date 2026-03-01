using BookingService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Status)
            .IsRequired();

        builder.Property(b => b.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.ExpiresAt)
            .IsRequired();

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.EventId);
        builder.HasIndex(b => new { b.Status, b.ExpiresAt });

        builder.HasMany(b => b.Items)
            .WithOne(i => i.Booking)
            .HasForeignKey(i => i.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

