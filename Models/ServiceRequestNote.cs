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

    /// <summary>Soft delete bayrağı. true ise listelerde görünmez.</summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>İsteğe bağlı: ne zaman silindi</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>İsteğe bağlı: kim sildi / neden</summary>
    [StringLength(200)]
    public string? DeletedBy { get; set; }
    [StringLength(400)]
    public string? DeleteReason { get; set; }

    [StringLength(120)]
    public string? CreatedBy { get; set; }    // admin kullanıcı adı

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(3);
}
