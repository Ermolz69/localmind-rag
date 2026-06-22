using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class CompanionDeviceConfiguration : IEntityTypeConfiguration<CompanionDevice>
{
    public void Configure(EntityTypeBuilder<CompanionDevice> builder)
    {
        builder.ToTable("companion_devices");

        builder.Property(device => device.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(device => device.Platform)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(device => device.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(device => device.TokenHash)
            .IsUnique();
    }
}
