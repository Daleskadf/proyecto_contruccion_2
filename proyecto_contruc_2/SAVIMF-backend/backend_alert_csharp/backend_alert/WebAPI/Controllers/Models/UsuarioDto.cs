using Google.Cloud.Firestore;

namespace WebAPI.Models
{
    [FirestoreData]
    public class UsuarioDto
    {
        [FirestoreProperty("uid")]
        public string Uid { get; set; } = string.Empty;
        [FirestoreProperty("dni")]
        public string Dni { get; set; } = string.Empty;
        [FirestoreProperty("nombre")]
        public string Nombre { get; set; } = string.Empty;
        [FirestoreProperty("apellido")]
        public string Apellido { get; set; } = string.Empty;
        [FirestoreProperty("edad")]
        public int Edad { get; set; } = 0;
        [FirestoreProperty("email")]
        public string Email { get; set; } = string.Empty;
        [FirestoreProperty("role")]
        public string Role { get; set; } = string.Empty;
        [FirestoreProperty("ordenJuez")]
        public bool OrdenJuez { get; set; } = false;
        [FirestoreProperty("device_id")]
        public string DeviceId { get; set; } = string.Empty;
    }
}