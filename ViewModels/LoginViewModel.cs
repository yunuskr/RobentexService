using System.ComponentModel.DataAnnotations;

namespace RobentexService.Models.ViewModels;

public class LoginViewModel
{
    [Required, Display(Name="Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name="Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name="Beni hatırla")]
    public bool RememberMe { get; set; }
}
