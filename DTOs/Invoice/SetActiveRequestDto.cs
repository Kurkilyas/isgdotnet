using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class SetActiveRequestDto
{
    [Required]
    public bool IsActive { get; set; }
}
