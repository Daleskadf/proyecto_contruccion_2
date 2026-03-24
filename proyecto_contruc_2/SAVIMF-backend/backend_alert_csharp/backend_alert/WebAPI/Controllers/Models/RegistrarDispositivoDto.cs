namespace WebAPI.Models
{
    public class RegistrarDispositivoDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DevEui { get; set; } = string.Empty;
        public string JoinEui { get; set; } = string.Empty;
        public string AppKey { get; set; } = string.Empty;
        // name y descripcion no los uso en el payload que envio para registrar un dispositivo en tts, 
        // pero lo voy a dejar porque quizas me sirva mas adelante
        //public string Name { get; set; } = string.Empty;
        //public string Description { get; set; } = string.Empty;
    }
}