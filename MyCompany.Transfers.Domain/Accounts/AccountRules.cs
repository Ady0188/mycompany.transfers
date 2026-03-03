using MyCompany.Transfers.Domain.Accounts.Enums;
using MyCompany.Transfers.Domain.Common;
using System.Text.RegularExpressions;

namespace MyCompany.Transfers.Domain.Accounts;

public static class AccountRules
{
    public static string Normalize(string? account, AccountDefinition def)
    {
        if (account is null) return string.Empty;
        var v = account;

        if (def.Normalize == AccountNormalizeMode.Trim)
            v = v.Trim();

        if (def.Normalize == AccountNormalizeMode.DigitsOnly)
            v = new string(v.Where(char.IsDigit).ToArray());

        return v;
    }

    public static void Validate(string account, AccountDefinition def)
    {
        if (string.IsNullOrWhiteSpace(account))
            throw new DomainException("Account is required.");

        if (def.MinLength.HasValue && account.Length < def.MinLength.Value)
            throw new DomainException($"Account is too short (min {def.MinLength}).");

        if (def.MaxLength.HasValue && account.Length > def.MaxLength.Value)
            throw new DomainException($"Account is too long (max {def.MaxLength}).");

        if (!string.IsNullOrWhiteSpace(def.Regex) &&
            !Regex.IsMatch(account, def.Regex!, RegexOptions.ECMAScript))
            throw new DomainException("Account format is invalid.");

        if (def.Algorithm == AccountAlgorithm.Luhn && !IsLuhnValid(account))
            throw new DomainException("Account checksum is invalid.");
    }

    private static bool IsLuhnValid(string digits)
    {
        int sum = 0;
        bool alt = false;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            char c = digits[i];
            if (c < '0' || c > '9') return false;

            int n = c - '0';
            if (alt)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alt = !alt;
        }

        return sum % 10 == 0;
    }
}
