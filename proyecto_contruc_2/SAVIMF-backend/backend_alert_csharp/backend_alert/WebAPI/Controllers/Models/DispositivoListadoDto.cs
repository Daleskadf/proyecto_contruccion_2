public class DispositivoListadoDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string DevEui { get; set; } = string.Empty;
    public string Vinculado { get; set; } = "NO"; // "SI" o "NO"
}