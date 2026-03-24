namespace backend_alert.WebAPI.Controllers.Models;

/// <summary>
/// DTO para registrar un nuevo atestado policial desde la app móvil
/// Pattern: Data Transfer Object (DTO)
/// </summary>
public class RegistrarAtestadoRequestDto
{
    public string AlertaId { get; set; } = string.Empty;
    public DateTime FechaIncidente { get; set; }
    
    // Ubicación
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string Distrito { get; set; } = string.Empty;
    public string? DireccionReferencial { get; set; }
    
    // Información del incidente
    public string TipoViolencia { get; set; } = string.Empty; // fisica, psicologica, sexual, economica
    public string NivelRiesgo { get; set; } = string.Empty; // bajo, medio, alto, critico
    public bool AlertaVeridica { get; set; }
    public string DescripcionHechos { get; set; } = string.Empty;
    
    // Datos de la víctima
    public string NombreVictima { get; set; } = string.Empty;
    public string DniVictima { get; set; } = string.Empty;
    public int EdadAproximada { get; set; }
    
    // Acciones tomadas
    public bool RequirioAmbulancia { get; set; }
    public bool RequirioRefuerzo { get; set; }
    public bool VictimaTrasladadaComisaria { get; set; }
    public string? AccionesRealizadas { get; set; }
    
    // Observaciones
    public string? Observaciones { get; set; }
}
