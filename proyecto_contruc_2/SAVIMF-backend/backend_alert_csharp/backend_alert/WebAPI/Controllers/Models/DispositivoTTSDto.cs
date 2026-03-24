using Google.Cloud.Firestore;

namespace WebAPI.Models
{
    [FirestoreData]
    public class DispositivoTTSDto
    {
        [FirestoreProperty]
        public string DeviceId { get; set; } = string.Empty;

        [FirestoreProperty]
        public string DevEui { get; set; } = string.Empty;

        [FirestoreProperty]
        public string JoinEui { get; set; } = string.Empty;

        [FirestoreProperty]
        public string AppKey { get; set; } = string.Empty;

        // Solo para Firestore:
        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Agrega más si necesitas, siempre con [FirestoreProperty]
    }
}