using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Accounts;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Rates;
using MyCompany.Transfers.Domain.Services;
using MyCompany.Transfers.Domain.Transfers;
using System.Text.Json;

namespace MyCompany.Transfers.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Terminal> Terminals => Set<Terminal>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceParamDefinition> ServiceParamDefinitions => Set<ServiceParamDefinition>();
    public DbSet<AccountDefinition> AccountDefinitions => Set<AccountDefinition>();
    public DbSet<ParamDefinition> Parameters => Set<ParamDefinition>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<Outbox> Outboxes => Set<Outbox>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<AgentCurrencyAccess> AgentCurrencies => Set<AgentCurrencyAccess>();
    public DbSet<AgentServiceAccess> AgentServices => Set<AgentServiceAccess>();
    public DbSet<FxRate> FxRates => Set<FxRate>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public Task CommitChangesAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    public async Task ExecuteTransactionalAsync(Func<CancellationToken, Task<bool>> work, CancellationToken ct = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await Database.BeginTransactionAsync(ct);
            var shouldCommit = await work(ct);
            if (shouldCommit)
            {
                await SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
        });
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var dictConv = new ValueConverter<Dictionary<string, long>, string>(
            v => JsonSerializer.Serialize(v ?? new(), (JsonSerializerOptions?)null),
            v => string.IsNullOrWhiteSpace(v)
                ? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
                : JsonSerializer.Deserialize<Dictionary<string, long>>(v, (JsonSerializerOptions?)null)!);

        var dictComparer = new ValueComparer<Dictionary<string, long>>(
            (a, b) => a != null && b != null && a.Count == b.Count &&
                a.OrderBy(kv => kv.Key).SequenceEqual(b.OrderBy(kv => kv.Key)),
            v => v == null ? 0 : v.Aggregate(0, (acc, kv) => HashCode.Combine(acc, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
            v => v == null ? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase) : v.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase));

        b.Entity<Agent>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Account).IsRequired().HasMaxLength(128);
            eb.Property(x => x.Balances).HasConversion(dictConv).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").Metadata.SetValueComparer(dictComparer);
            eb.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(64).HasDefaultValue("Asia/Dushanbe");
            eb.Property(x => x.SettingsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        });

        b.Entity<Terminal>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.AgentId).IsRequired().HasMaxLength(64);
            eb.Property(x => x.ApiKey).IsRequired().HasMaxLength(128);
            eb.HasIndex(x => x.ApiKey).IsUnique();
        });

        b.Entity<ParamDefinition>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Id).HasMaxLength(64);
            eb.Property(x => x.Code).HasMaxLength(64).IsRequired();
            eb.ToTable("ParamDefinition");
        });

        b.Entity<ServiceParamDefinition>(eb =>
        {
            eb.HasKey(x => new { x.ServiceId, x.ParameterId });
            eb.Property(x => x.ServiceId).HasMaxLength(128);
            eb.Property(x => x.ParameterId).HasMaxLength(64);
            eb.HasOne(x => x.Parameter).WithMany().HasForeignKey(x => x.ParameterId).OnDelete(DeleteBehavior.Restrict);
            eb.ToTable("ServiceParamDefinition");
        });

        b.Entity<Service>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.ProviderId).HasMaxLength(64);
            eb.Property(x => x.ProviderServiceId).HasMaxLength(64);
            eb.Property(x => x.Name).HasMaxLength(128);
            eb.Property(x => x.FxRounding).HasMaxLength(16);
            eb.Property(x => x.AllowedCurrencies).HasConversion(v => string.Join(',', v), v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            eb.HasMany(x => x.Parameters).WithOne().HasForeignKey(p => p.ServiceId).OnDelete(DeleteBehavior.Cascade);
            eb.ToTable("Services");
        });

        var dictConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new());

        ConfigureTransfer(b, dictConverter);
        ConfigureOutbox(b, dictConverter);

        b.Entity<AgentCurrencyAccess>(eb =>
        {
            eb.HasKey(x => new { x.AgentId, x.Currency });
            eb.Property(x => x.AgentId).HasMaxLength(64).IsRequired();
            eb.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            eb.ToTable("AgentCurrencyAccess");
        });

        b.Entity<AgentServiceAccess>(eb =>
        {
            eb.HasKey(x => new { x.AgentId, x.ServiceId });
            eb.Property(x => x.AgentId).HasMaxLength(64).IsRequired();
            eb.Property(x => x.ServiceId).HasMaxLength(128).IsRequired();
            eb.Property(x => x.FeePermille).HasDefaultValue(0);
            eb.Property(x => x.FeeFlatMinor).HasDefaultValue(0L);
            eb.ToTable("AgentServiceAccess");
        });

        b.Entity<Provider>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Id).HasMaxLength(64).IsRequired();
            eb.Property(x => x.Account).IsRequired().HasMaxLength(128);
            eb.Property(x => x.Name).HasMaxLength(128).IsRequired();
            eb.Property(x => x.BaseUrl).HasMaxLength(512).IsRequired();
            eb.Property(x => x.TimeoutSeconds).HasDefaultValue(30);
            eb.Property(x => x.AuthType).HasConversion<string>().HasMaxLength(32);
            eb.Property(x => x.SettingsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            eb.Property(x => x.IsEnabled).HasDefaultValue(true);
            eb.Property(x => x.IsOnline).HasDefaultValue(false);
            eb.Property(x => x.FeePermille).HasDefaultValue(0);
            eb.ToTable("Providers");
        });

        b.Entity<FxRate>(eb =>
        {
            eb.ToTable("FxRates");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.AgentId).HasMaxLength(64).IsRequired();
            eb.Property(x => x.BaseCurrency).HasMaxLength(3).IsRequired();
            eb.Property(x => x.QuoteCurrency).HasMaxLength(3).IsRequired();
            eb.Property(x => x.Rate).HasColumnType("numeric(20,8)").IsRequired();
            eb.Property(x => x.UpdatedAtUtc).IsRequired();
            eb.Property(x => x.Source).HasMaxLength(32).HasDefaultValue("manual");
            eb.Property(x => x.IsActive).HasDefaultValue(true);
            eb.HasIndex(x => new { x.AgentId, x.BaseCurrency, x.QuoteCurrency }).IsUnique();
        });

        b.Entity<AccountDefinition>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Code).HasMaxLength(64).IsRequired();
            eb.ToTable("AccountDefinitions");
        });
    }

    private static void ConfigureTransfer(ModelBuilder b, ValueConverter<Dictionary<string, string>, string> dictConverter)
    {
        b.Entity<Transfer>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.NumId).ValueGeneratedOnAdd();
            eb.HasIndex(x => x.NumId).IsUnique();
            eb.Property(x => x.AgentId).HasMaxLength(64);
            eb.Property(x => x.TerminalId).HasMaxLength(64);
            eb.Property(x => x.ExternalId).HasMaxLength(128);
            eb.Property(x => x.ServiceId).HasMaxLength(128);
            eb.Property(x => x.Account).HasMaxLength(128);
            eb.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            eb.Property(x => x.Method).HasConversion<string>().HasMaxLength(16);
            eb.OwnsOne(x => x.Amount, m =>
            {
                m.Property(p => p.Minor).HasColumnName("AmountMinor");
                m.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            eb.OwnsOne(x => x.CurrentQuote, q =>
            {
                q.Property(p => p.Id).HasColumnName("QuoteId");
                q.OwnsOne(p => p.Total, m => { m.Property(pp => pp.Minor).HasColumnName("TotalMinor"); m.Property(pp => pp.Currency).HasColumnName("TotalCurrency").HasMaxLength(3); });
                q.OwnsOne(p => p.Fee, m => { m.Property(pp => pp.Minor).HasColumnName("FeeMinor"); m.Property(pp => pp.Currency).HasColumnName("FeeCurrency").HasMaxLength(3); });
                q.OwnsOne(p => p.CreditedAmount, m => { m.Property(pp => pp.Minor).HasColumnName("CreditedMinor"); m.Property(pp => pp.Currency).HasColumnName("CreditedCurrency").HasMaxLength(3); });
                q.OwnsOne(p => p.ProviderFee, m => { m.Property(pp => pp.Minor).HasColumnName("ProviderFeeMinor"); m.Property(pp => pp.Currency).HasColumnName("ProviderFeeCurrency").HasMaxLength(3); });
                q.Property(p => p.ExchangeRate).HasColumnName("ExchangeRate").HasColumnType("numeric(20,8)");
                q.Property(p => p.RateTimestampUtc).HasColumnName("RateTimestampUtc");
                q.Property(p => p.ExpiresAt).HasColumnName("QuoteExpiresAtUtc");
            });
            eb.Property<Dictionary<string, string>>("_parameters").HasConversion(dictConverter).HasColumnName("Parameters").HasColumnType("jsonb");
            eb.Property<Dictionary<string, string>>("_provReceivedParams").HasConversion(dictConverter).HasColumnName("ProvReceivedParams").HasColumnType("jsonb");
            eb.Metadata.FindNavigation(nameof(Transfer.Parameters))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            eb.HasIndex(x => new { x.AgentId, x.ExternalId }).IsUnique();
        });
    }

    private static void ConfigureOutbox(ModelBuilder b, ValueConverter<Dictionary<string, string>, string> dictConverter)
    {
        b.Entity<Outbox>(eb =>
        {
            eb.HasKey(x => x.TransferId);
            eb.Property(x => x.AgentId).HasMaxLength(64);
            eb.Property(x => x.TerminalId).HasMaxLength(64);
            eb.Property(x => x.ExternalId).HasMaxLength(128);
            eb.Property(x => x.ServiceId).HasMaxLength(128);
            eb.Property(x => x.Account).HasMaxLength(128);
            eb.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            eb.Property(x => x.Method).HasConversion<string>().HasMaxLength(16);
            eb.OwnsOne(x => x.Amount, m => { m.Property(p => p.Minor).HasColumnName("AmountMinor"); m.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3); });
            eb.OwnsOne(x => x.CurrentQuote, q =>
            {
                q.Property(p => p.Id).HasColumnName("QuoteId");
                q.OwnsOne(p => p.Total, m => { m.Property(pp => pp.Minor).HasColumnName("TotalMinor"); m.Property(pp => pp.Currency).HasColumnName("TotalCurrency").HasMaxLength(3); });
                q.OwnsOne(p => p.Fee, m => { m.Property(pp => pp.Minor).HasColumnName("FeeMinor"); m.Property(pp => pp.Currency).HasColumnName("FeeCurrency").HasMaxLength(3); });
                q.OwnsOne(p => p.CreditedAmount, m => { m.Property(pp => pp.Minor).HasColumnName("CreditedMinor"); m.Property(pp => pp.Currency).HasColumnName("CreditedCurrency").HasMaxLength(3); });
                q.OwnsOne(p => p.ProviderFee, m => { m.Property(pp => pp.Minor).HasColumnName("ProviderFeeMinor"); m.Property(pp => pp.Currency).HasColumnName("ProviderFeeCurrency").HasMaxLength(3); });
                q.Property(p => p.ExchangeRate).HasColumnName("ExchangeRate").HasColumnType("numeric(20,8)");
                q.Property(p => p.RateTimestampUtc).HasColumnName("RateTimestampUtc");
                q.Property(p => p.ExpiresAt).HasColumnName("QuoteExpiresAtUtc");
            });
            eb.Property<Dictionary<string, string>>("_parameters").HasConversion(dictConverter).HasColumnName("Parameters").HasColumnType("jsonb");
            eb.Property<Dictionary<string, string>>("_provReceivedParams").HasConversion(dictConverter).HasColumnName("ProvReceivedParams").HasColumnType("jsonb");
            eb.Metadata.FindNavigation(nameof(Outbox.Parameters))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            eb.HasIndex(x => new { x.AgentId, x.ExternalId }).IsUnique();
        });
    }
}
