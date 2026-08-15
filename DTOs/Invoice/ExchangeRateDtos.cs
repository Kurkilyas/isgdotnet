using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class ExchangeRateResponseDto
{
    public int Id { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public DateOnly RateDate { get; set; }
    public decimal RateToTry { get; set; }
    public string Source { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class CreateExchangeRateRequestDto
{
    [Required, MaxLength(10)]
    public string CurrencyCode { get; set; } = null!;

    [Required]
    public DateOnly RateDate { get; set; }

    [Required, Range(0.000001, double.MaxValue)]
    public decimal RateToTry { get; set; }

    [MaxLength(30)]
    public string Source { get; set; } = "TCMB";
}
