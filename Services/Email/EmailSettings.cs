namespace RobentexService.Services.Email;

public sealed class EmailSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";
    public string FromName { get; set; } = "Robentex";
    public bool UseStartTls { get; set; } = true;
}
