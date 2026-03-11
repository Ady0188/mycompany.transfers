using System.ComponentModel.DataAnnotations;

namespace MyCompany.Transfers.Contract.Solidarnost.Requests;

/// <summary>
/// Запрос протокола Solidarnost (payment_app). Action: nmtcheck, clientcheck, payment, paycheck.
/// </summary>
public class TransferRequest
{
    [Required]
    public string Action { get; set; } = string.Empty;

    public string? Account { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Settlement_Curr { get; set; }
    public string? Id_Series_Number { get; set; }
    public string? Sender_Fio { get; set; }
    public string? Sender_Birthday { get; set; }
    public decimal? Curr_Rate { get; set; }
    public string? Pay_Id { get; set; }
    public string? Pay_Date { get; set; }
    public string? Partner_Id { get; set; }
    public string? ExtId { get; set; }
    public string? Sign { get; set; }
}
