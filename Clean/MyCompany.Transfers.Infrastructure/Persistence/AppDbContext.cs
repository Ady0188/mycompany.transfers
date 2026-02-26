using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Accounts;
using MyCompany.Transfers.Infrastructure.Encryption;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Rates;
using MyCompany.Transfers.Domain.Services;
using MyCompany.Transfers.Domain.Transfers;
using System.Text.Json;
using MyCompany.Transfers.Domain.Accounts.Enums;

namespace MyCompany.Transfers.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentBalanceHistory> AgentBalanceHistories => Set<AgentBalanceHistory>();
    public DbSet<AgentDailyBalance> AgentDailyBalances => Set<AgentDailyBalance>();
    public DbSet<Terminal> Terminals => Set<Terminal>();
    public DbSet<SentCredentialsEmail> SentCredentialsEmails => Set<SentCredentialsEmail>();
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
            eb.Property(x => x.Name).HasMaxLength(256);
            eb.Property(x => x.Account).IsRequired().HasMaxLength(128);
            eb.Property(x => x.Balances).HasConversion(dictConv).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").Metadata.SetValueComparer(dictComparer);
            eb.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(64).HasDefaultValue("Asia/Dushanbe");
            eb.Property(x => x.SettingsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            eb.Property(x => x.PartnerEmail).HasMaxLength(256);
            eb.Property(x => x.Locale).IsRequired().HasMaxLength(8).HasDefaultValue("ru");
        });

        b.Entity<AgentBalanceHistory>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.AgentId).HasMaxLength(64).IsRequired();
            eb.Property(x => x.DocId).IsRequired(false);
            eb.Property(x => x.CreatedAtUtc).IsRequired();
            eb.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            eb.Property(x => x.CurrentBalanceMinor).HasColumnType("bigint").IsRequired();
            eb.Property(x => x.IncomeMinor).HasColumnType("bigint").IsRequired();
            eb.Property(x => x.NewBalanceMinor).HasColumnType("bigint").IsRequired();
            eb.Property(x => x.ReferenceType).HasConversion<string>().HasMaxLength(16).IsRequired();
            eb.Property(x => x.ReferenceId).HasMaxLength(128).IsRequired();

            eb.HasIndex(x => new { x.AgentId, x.Currency, x.ReferenceType, x.ReferenceId })
                .IsUnique()
                .HasDatabaseName("IX_AgentBalanceHistory_AgentId_Currency_ReferenceType_ReferenceId");
            eb.HasIndex(x => new { x.AgentId, x.Currency, x.CreatedAtUtc })
                .HasDatabaseName("IX_AgentBalanceHistory_AgentId_Currency_CreatedAtUtc");

            eb.ToTable("AgentBalanceHistory");
        });

        b.Entity<AgentDailyBalance>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.AgentId).HasMaxLength(64).IsRequired();
            eb.Property(x => x.Date).IsRequired();
            eb.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            eb.Property(x => x.OpeningBalanceMinor).HasColumnType("bigint").IsRequired();
            eb.Property(x => x.ClosingBalanceMinor).HasColumnType("bigint").IsRequired();
            eb.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
            eb.Property(x => x.Scope).HasConversion<string>().HasMaxLength(16).IsRequired();

            eb.HasIndex(x => new { x.AgentId, x.Currency, x.Date, x.TimeZoneId, x.Scope })
                .IsUnique()
                .HasDatabaseName("IX_AgentDailyBalance_AgentId_Currency_Date_TimeZoneId_Scope");
            eb.HasIndex(x => new { x.AgentId, x.Date })
                .HasDatabaseName("IX_AgentDailyBalance_AgentId_Date");

            eb.ToTable("AgentDailyBalance");
        });

        b.Entity<Terminal>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.AgentId).IsRequired().HasMaxLength(64);
            var enc = CredentialsEncryptionHolder.Encryption;
            if (enc != null)
            {
                eb.Property(x => x.ApiKey).IsRequired()
                    .HasConversion(v => enc.EncryptApiKey(v), v => enc.DecryptApiKey(v))
                    .HasMaxLength(512);
                eb.Property(x => x.Secret).HasConversion(v => enc.EncryptSecret(v), v => enc.DecryptSecret(v)).HasMaxLength(1024);
            }
            else
            {
                eb.Property(x => x.ApiKey).IsRequired().HasMaxLength(128);
            }
            eb.HasIndex(x => x.ApiKey).IsUnique();
        });

        b.Entity<SentCredentialsEmail>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.AgentId).IsRequired().HasMaxLength(64);
            eb.Property(x => x.TerminalId).IsRequired().HasMaxLength(64);
            eb.Property(x => x.ToEmail).IsRequired().HasMaxLength(256);
            eb.Property(x => x.Subject).IsRequired().HasMaxLength(512);
            eb.Property(x => x.SentAtUtc).IsRequired();
            eb.ToTable("SentCredentialsEmails");
        });

        b.Entity<ParamDefinition>(eb =>
        {
            eb.HasKey(x => x.Id);
            eb.Property(x => x.Id).HasMaxLength(64);
            eb.Property(x => x.Code).HasMaxLength(64).IsRequired();
            eb.ToTable("ParamDefinition");
            eb.HasData(
                new { Id = "100", Code = "sender_doc_type", Name = "Тип удостоверяющего документа плательщика", Description = (string?)null, Regex = (string?)null, Active = true },
                new { Id = "101", Code = "sender_doc_number", Name = "Серия и номер документа", Description = (string?)null, Regex = "^[A-Z0-9]+$", Active = true },
                new { Id = "102", Code = "sender_phone", Name = "Номер мобильного телефона плательщика", Description = (string?)null, Regex = (string?)null, Active = true },
                new { Id = "103", Code = "sender_doc_department_code", Name = "Код подразделения, выдавшего паспорт", Description = (string?)null, Regex = (string?)null, Active = true },
                new { Id = "104", Code = "sender_residency", Name = "Резидентство плательщика: 1 – резидент РТ, 0 – нерезидент.", Description = (string?)null, Regex = "^(1{1}|2{1})$", Active = true },
                new { Id = "105", Code = "account_number", Name = "Номер счёта плательщика", Description = (string?)null, Regex = (string?)null, Active = true },
                new { Id = "106", Code = "sender_doc_issue_date", Name = "Дата выдачи документа", Description = (string?)null, Regex = "^(19|20)\\d{2}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$", Active = true },
                new { Id = "107", Code = "sender_fullname_cyr", Name = "Полное ФИО плательщика (кириллицей)", Description = (string?)null, Regex = "^[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*\\s[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*(?:\\s[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*)?$", Active = true },
                new { Id = "108", Code = "sender_firstname_cyr", Name = "Фамилия плательщика (кириллицей)", Description = (string?)null, Regex = "^[А-Яа-яЁё\\s\\-]+$", Active = true },
                new { Id = "109", Code = "sender_lastname_cyr", Name = "Имя плательщика (кириллицей)", Description = (string?)null, Regex = "^[А-Яа-яЁё\\s\\-]+$", Active = true },
                new { Id = "110", Code = "sender_middlename_cyr", Name = "Отчество плательщика (кириллицей)", Description = (string?)null, Regex = "^[А-Яа-яЁё\\s\\-]+$", Active = true },
                new { Id = "111", Code = "receiver_fullname_cyr", Name = "Полное ФИО получателя (кириллицей)", Description = (string?)null, Regex = "^[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*\\s[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*(?:\\s[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*)?$", Active = true },
                new { Id = "112", Code = "receiver_firstname_cyr", Name = "Имя получателя (кириллицей)", Description = (string?)null, Regex = "^[А-Яа-яЁё\\s\\-]+$", Active = true },
                new { Id = "113", Code = "receiver_lastname_cyr", Name = "Фамилия получателя (кириллицей)", Description = (string?)null, Regex = "^[А-Яа-яЁё\\s\\-]+$", Active = true },
                new { Id = "114", Code = "receiver_middlename_cyr", Name = "Отчество получателя (кириллицей)", Description = (string?)null, Regex = "^[А-Яа-яЁё\\s\\-]+$", Active = true },
                new { Id = "115", Code = "sender_fullname", Name = "Полное ФИО плательщика \"Фамилия Имя Отчество\"", Description = (string?)null, Regex = "^[A-Za-z]+(?:-[A-Za-z]+)*\\s[A-Za-z]+(?:-[A-Za-z]+)*(?:\\s[A-Za-z]+(?:-[A-Za-z]+)*)?$", Active = true },
                new { Id = "116", Code = "sender_lastname", Name = "Фамилия плательщика", Description = (string?)null, Regex = "^[A-Za-z\\s\\-]+$", Active = true },
                new { Id = "117", Code = "sender_firstname", Name = "Имя плательщика", Description = (string?)null, Regex = "^[A-Za-z\\s\\-]+$", Active = true },
                new { Id = "118", Code = "sender_middlename", Name = "Отчество плательщика", Description = (string?)null, Regex = "^[A-Za-z\\s\\-]+$", Active = true },
                new { Id = "119", Code = "sender_doc_issuer", Name = "Орган, выдавший документ", Description = (string?)null, Regex = (string?)null, Active = true },
                new { Id = "120", Code = "sender_birth_place", Name = "Место рождения плательщика", Description = (string?)null, Regex = (string?)null, Active = true },
                new { Id = "121", Code = "sender_citizenship", Name = "Гражданство плательщика", Description = (string?)null, Regex = (string?)null, Active = true },
                new { Id = "122", Code = "sender_registration_address", Name = "Адрес регистрации: страна, регион, город, улица, дом, квартира и т.п.", Description = (string?)null, Regex = (string?)null, Active = true },
                new { Id = "123", Code = "receiver_fullname", Name = "Полное ФИО получателя", Description = (string?)null, Regex = "^[A-Za-z]+(?:-[A-Za-z]+)*\\s[A-Za-z]+(?:-[A-Za-z]+)*(?:\\s[A-Za-z]+(?:-[A-Za-z]+)*)?$", Active = true },
                new { Id = "124", Code = "receiver_firstname", Name = "Имя получателя", Description = (string?)null, Regex = "^[A-Za-z\\s\\-]+$", Active = true },
                new { Id = "125", Code = "receiver_lastname", Name = "Фамилия получателя", Description = (string?)null, Regex = "^[A-Za-z\\s\\-]+$", Active = true },
                new { Id = "126", Code = "receiver_middlename", Name = "Отчество получателя", Description = (string?)null, Regex = "^[A-Za-z\\s\\-]+$", Active = true },
                new { Id = "127", Code = "sender_birth_date", Name = "Дата рождения плательщика", Description = (string?)null, Regex = "^(19|20)\\d{2}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$", Active = true }
            );
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
            eb.HasData(
                new { Id = Guid.Parse("1744710c-a062-4c33-8077-756db29a06b6"), Code = "PHONE", Regex = "^\\d{9,15}$", Normalize = AccountNormalizeMode.DigitsOnly, Algorithm = AccountAlgorithm.None, MinLength = 9, MaxLength = 15 },
                new { Id = Guid.Parse("3fcdac91-5f18-425b-945f-169d539b6bde"), Code = "PAN", Regex = "^\\d{16}$", Normalize = AccountNormalizeMode.DigitsOnly, Algorithm = AccountAlgorithm.Luhn, MinLength = 16, MaxLength = 16 }
            );
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
