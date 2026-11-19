using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        // SQLite drops the DateTimeKind; force UTC on read so the API emits ISO 8601 with the trailing 'Z'.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookedBy).IsRequired().HasMaxLength(200);
        builder.Property(b => b.StartUtc).IsRequired().HasConversion(utcConverter);
        builder.Property(b => b.EndUtc).IsRequired().HasConversion(utcConverter);
        builder.Property(b => b.Attendees).IsRequired();
        builder.Property(b => b.Status).IsRequired().HasConversion<string>();

        builder.HasOne<Room>().WithMany()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.RoomId);
    }
}
