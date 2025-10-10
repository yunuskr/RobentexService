using System.ComponentModel.DataAnnotations;
using RobentexService.Models;

namespace RobentexService.Models.ViewModels;

public class ServiceRequestAdminEditVM
{
    public int Id { get; set; }

    // Ekranda gösterilecek kullanıcı alanları (salt okunur gösterebilirsin)
    public string? CompanyName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? RobotModel { get; set; }
    public string? RobotSerial { get; set; }

    // Admin’in düzenleyeceği alanlar
    [Display(Name="Başlık")]
    public string? Title { get; set; }

    [Display(Name="Takip No")]
    public string? TrackingNo { get; set; }

    [Display(Name="Müşteri Sipariş No")]
    public string? CustomerOrderNo { get; set; }

    [Display(Name = "Robentex Sipariş No")]
    public string? RobentexOrderNo { get; set; }
    
    [Display(Name="Arıza Tanımı")]
    public string? FaultDescription { get; set; }

    [Display(Name="Durum/Bayrak")]
    public ServiceStatus Status { get; set; }

    // Yeni not (opsiyonel)
    [Display(Name="Not Ekle")]
    public string? NewNote { get; set; }

    // Mevcut notlar (liste)
    public List<(DateTime createdAt, string? createdBy, string text)> Notes { get; set; } = new();
}
