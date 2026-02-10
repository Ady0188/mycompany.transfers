using MyCompany.Transfers.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Configurations;

public sealed class AccountDefinitionConfiguration
    : IEntityTypeConfiguration<AccountDefinition>
{
    public void Configure(EntityTypeBuilder<AccountDefinition> builder)
    {
        builder.ToTable("AccountDefinitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Regex)
            .HasMaxLength(500);

        builder.Property(x => x.Normalize)
            .IsRequired();

        builder.Property(x => x.Algorithm)
            .IsRequired();

        builder.Property(x => x.MinLength);
        builder.Property(x => x.MaxLength);
    }
}