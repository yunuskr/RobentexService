using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RobentexService.Models;

public class ServiceRequestNote
{
    public int Id { get; set; }

    [Required]
    public int ServiceRequestId { get; set; }

    [ForeignKey(nameof(ServiceRequestId))]
    public ServiceRequest? ServiceRequest { get; set; }

    [Required, StringLength(2000)]
    public string Text { get; set; } = string.Empty;

    [StringLength(120)]
    public string? CreatedBy { get; set; }    // admin kullanıcı adı

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(3);
}
