using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace RobentexService.Models;

public class ServiceRequest
{

    
    // ----- Kullanıcı Tarafından Girilecek Olan Veriler -----
    public int Id { get; set; }

    [Required, Display(Name = "Ad")]
    [StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, Display(Name = "Soyad")]
    [StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required,Display(Name = "Robot Seri No")]
    [StringLength(80)]
    public string? RobotSerial { get; set; }

    [Required,Display(Name = "Robot Model")]
    [StringLength(80)]
    public string? RobotModel { get; set; }

    [Required,Display(Name = "Firma Adı")]
    [StringLength(120)]
    public string? CompanyName { get; set; }

    [Required,Display(Name = "Arıza Tanımı")]
    [StringLength(2000)]
    public string? FaultDescription { get; set; }

    [Required,EmailAddress, Display(Name = "E-posta")]
    [StringLength(200)]
    public string? Email { get; set; }

    [Required,Phone, Display(Name = "Tel")]
    [StringLength(40)]
    public string? Phone { get; set; }

    [Display(Name = "Kayıt Zamanı")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped] 
    public string ?Website { get; set; }

    // ----- Admin Tarafından Girilecek Olan Veriler -----
    
    [Display(Name="Başlık"), StringLength(200)]
    public string? Title { get; set; }

    [Display(Name="Takip No"), StringLength(80)]
    public string? TrackingNo { get; set; }

    [Display(Name="Müşteri Sipariş No"), StringLength(80)]
    public string? CustomerOrderNo { get; set; }

    [Display(Name="Robentex Sipariş No"), StringLength(80)]
    public string? RobentexOrderNo { get; set; }

    [Display(Name="Durum/Bayrak")]
    public ServiceStatus Status { get; set; } = ServiceStatus.YeniTalep;

    [Display(Name="Güncelleme")]
    public DateTime? UpdatedAt { get; set; }


    /// <summary>Soft delete bayrağı. true ise listelerde görünmez.</summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>İsteğe bağlı: ne zaman silindi</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>İsteğe bağlı: kim sildi / neden</summary>
    [StringLength(200)]
    public string? DeletedBy { get; set; }
    [StringLength(400)]
    public string? DeleteReason { get; set; }

    // Notlar (çoklu)
    public ICollection<ServiceRequestNote> Notes { get; set; } = new List<ServiceRequestNote>();
}
