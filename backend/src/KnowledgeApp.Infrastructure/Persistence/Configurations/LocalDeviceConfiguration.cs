using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class LocalDeviceConfiguration : IEntityTypeConfiguration<LocalDevice>
{
    public void Configure(EntityTypeBuilder<LocalDevice> builder)
    {
        builder.ToTable("local_devices");
        builder.HasIndex(device => device.DeviceKey).IsUnique();
    }
}
