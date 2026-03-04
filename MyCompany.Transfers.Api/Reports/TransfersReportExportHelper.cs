using System.Globalization;
using System.Text;
using MyCompany.Transfers.Application.Reports.Transfers;
using ClosedXML.Excel;

namespace MyCompany.Transfers.Api.Reports;

public static class TransfersReportExportHelper
{
    private static string CsvEscape(string? value)
    {
        if (value == null) return "";
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    public static (byte[] Content, string ContentType, string FileName) ToCsv(
        TransfersReportResult<TransfersByPeriodReportItemDto> result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Период с;Период по;Кол-во;Сумма (минор);Валюта;Комиссия (минор);Валюта ком.;Ком. провайдера (минор);Валюта ком. пр.");
        foreach (var r in result.Items)
        {
            sb.AppendLine(string.Join(";",
                r.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.TransfersCount,
                r.AmountMinor,
                CsvEscape(r.AmountCurrency),
                r.FeeMinor,
                CsvEscape(r.FeeCurrency),
                r.ProviderFeeMinor,
                CsvEscape(r.ProviderFeeCurrency)));
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return (bytes, "text/csv; charset=utf-8", "report-by-period.csv");
    }

    public static (byte[] Content, string ContentType, string FileName) ToCsv(
        TransfersReportResult<TransfersByAgentReportItemDto> result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Агент ID;Агент;Кол-во;Сумма (минор);Валюта;Комиссия (минор);Валюта ком.;Ком. провайдера (минор);Валюта ком. пр.");
        foreach (var r in result.Items)
        {
            sb.AppendLine(string.Join(";",
                CsvEscape(r.AgentId),
                CsvEscape(r.AgentName ?? r.AgentId),
                r.TransfersCount,
                r.AmountMinor,
                CsvEscape(r.AmountCurrency),
                r.FeeMinor,
                CsvEscape(r.FeeCurrency),
                r.ProviderFeeMinor,
                CsvEscape(r.ProviderFeeCurrency)));
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return (bytes, "text/csv; charset=utf-8", "report-by-agent.csv");
    }

    public static (byte[] Content, string ContentType, string FileName) ToCsv(
        TransfersReportResult<TransfersByProviderReportItemDto> result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Провайдер ID;Провайдер;Кол-во;Сумма (минор);Валюта;Комиссия (минор);Валюта ком.;Ком. провайдера (минор);Валюта ком. пр.");
        foreach (var r in result.Items)
        {
            sb.AppendLine(string.Join(";",
                CsvEscape(r.ProviderId),
                CsvEscape(r.ProviderName ?? r.ProviderId),
                r.TransfersCount,
                r.AmountMinor,
                CsvEscape(r.AmountCurrency),
                r.FeeMinor,
                CsvEscape(r.FeeCurrency),
                r.ProviderFeeMinor,
                CsvEscape(r.ProviderFeeCurrency)));
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return (bytes, "text/csv; charset=utf-8", "report-by-provider.csv");
    }

    public static (byte[] Content, string ContentType, string FileName) ToCsv(
        TransfersReportResult<TransfersRevenueReportItemDto> result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Период с;Период по;Кол-во;Комиссии всего (минор);Ком. провайдеров (минор);Маржа (минор);Валюта");
        foreach (var r in result.Items)
        {
            sb.AppendLine(string.Join(";",
                r.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.TransfersCount,
                r.TotalFeeMinor,
                r.TotalProviderFeeMinor,
                r.MarginMinor,
                CsvEscape(r.Currency)));
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return (bytes, "text/csv; charset=utf-8", "report-revenue.csv");
    }

    public static (byte[] Content, string ContentType, string FileName) ToExcel(
        TransfersReportResult<TransfersByPeriodReportItemDto> result)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("По периодам");
        ws.Cell(1, 1).Value = "Период с"; ws.Cell(1, 2).Value = "Период по"; ws.Cell(1, 3).Value = "Кол-во";
        ws.Cell(1, 4).Value = "Сумма (минор)"; ws.Cell(1, 5).Value = "Валюта"; ws.Cell(1, 6).Value = "Комиссия (минор)";
        ws.Cell(1, 7).Value = "Валюта ком."; ws.Cell(1, 8).Value = "Ком. провайдера (минор)"; ws.Cell(1, 9).Value = "Валюта ком. пр.";
        int row = 2;
        foreach (var r in result.Items)
        {
            ws.Cell(row, 1).Value = r.PeriodStart; ws.Cell(row, 2).Value = r.PeriodEnd; ws.Cell(row, 3).Value = r.TransfersCount;
            ws.Cell(row, 4).Value = r.AmountMinor; ws.Cell(row, 5).Value = r.AmountCurrency; ws.Cell(row, 6).Value = r.FeeMinor;
            ws.Cell(row, 7).Value = r.FeeCurrency; ws.Cell(row, 8).Value = r.ProviderFeeMinor; ws.Cell(row, 9).Value = r.ProviderFeeCurrency;
            row++;
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms, false);
        return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report-by-period.xlsx");
    }

    public static (byte[] Content, string ContentType, string FileName) ToExcel(
        TransfersReportResult<TransfersByAgentReportItemDto> result)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("По агентам");
        ws.Cell(1, 1).Value = "Агент ID"; ws.Cell(1, 2).Value = "Агент"; ws.Cell(1, 3).Value = "Кол-во";
        ws.Cell(1, 4).Value = "Сумма (минор)"; ws.Cell(1, 5).Value = "Валюта"; ws.Cell(1, 6).Value = "Комиссия (минор)";
        ws.Cell(1, 7).Value = "Валюта ком."; ws.Cell(1, 8).Value = "Ком. провайдера (минор)"; ws.Cell(1, 9).Value = "Валюта ком. пр.";
        int row = 2;
        foreach (var r in result.Items)
        {
            ws.Cell(row, 1).Value = r.AgentId; ws.Cell(row, 2).Value = r.AgentName ?? r.AgentId; ws.Cell(row, 3).Value = r.TransfersCount;
            ws.Cell(row, 4).Value = r.AmountMinor; ws.Cell(row, 5).Value = r.AmountCurrency; ws.Cell(row, 6).Value = r.FeeMinor;
            ws.Cell(row, 7).Value = r.FeeCurrency; ws.Cell(row, 8).Value = r.ProviderFeeMinor; ws.Cell(row, 9).Value = r.ProviderFeeCurrency;
            row++;
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms, false);
        return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report-by-agent.xlsx");
    }

    public static (byte[] Content, string ContentType, string FileName) ToExcel(
        TransfersReportResult<TransfersByProviderReportItemDto> result)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("По провайдерам");
        ws.Cell(1, 1).Value = "Провайдер ID"; ws.Cell(1, 2).Value = "Провайдер"; ws.Cell(1, 3).Value = "Кол-во";
        ws.Cell(1, 4).Value = "Сумма (минор)"; ws.Cell(1, 5).Value = "Валюта"; ws.Cell(1, 6).Value = "Комиссия (минор)";
        ws.Cell(1, 7).Value = "Валюта ком."; ws.Cell(1, 8).Value = "Ком. провайдера (минор)"; ws.Cell(1, 9).Value = "Валюта ком. пр.";
        int row = 2;
        foreach (var r in result.Items)
        {
            ws.Cell(row, 1).Value = r.ProviderId; ws.Cell(row, 2).Value = r.ProviderName ?? r.ProviderId; ws.Cell(row, 3).Value = r.TransfersCount;
            ws.Cell(row, 4).Value = r.AmountMinor; ws.Cell(row, 5).Value = r.AmountCurrency; ws.Cell(row, 6).Value = r.FeeMinor;
            ws.Cell(row, 7).Value = r.FeeCurrency; ws.Cell(row, 8).Value = r.ProviderFeeMinor; ws.Cell(row, 9).Value = r.ProviderFeeCurrency;
            row++;
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms, false);
        return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report-by-provider.xlsx");
    }

    public static (byte[] Content, string ContentType, string FileName) ToExcel(
        TransfersReportResult<TransfersRevenueReportItemDto> result)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Доходность");
        ws.Cell(1, 1).Value = "Период с"; ws.Cell(1, 2).Value = "Период по"; ws.Cell(1, 3).Value = "Кол-во";
        ws.Cell(1, 4).Value = "Комиссии всего (минор)"; ws.Cell(1, 5).Value = "Ком. провайдеров (минор)"; ws.Cell(1, 6).Value = "Маржа (минор)"; ws.Cell(1, 7).Value = "Валюта";
        int row = 2;
        foreach (var r in result.Items)
        {
            ws.Cell(row, 1).Value = r.PeriodStart; ws.Cell(row, 2).Value = r.PeriodEnd; ws.Cell(row, 3).Value = r.TransfersCount;
            ws.Cell(row, 4).Value = r.TotalFeeMinor; ws.Cell(row, 5).Value = r.TotalProviderFeeMinor; ws.Cell(row, 6).Value = r.MarginMinor; ws.Cell(row, 7).Value = r.Currency;
            row++;
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms, false);
        return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report-revenue.xlsx");
    }
}
