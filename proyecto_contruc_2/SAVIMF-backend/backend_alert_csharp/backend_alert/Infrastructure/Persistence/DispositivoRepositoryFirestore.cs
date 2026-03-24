using System.Threading.Tasks;
using Domain.Interfaces;
using Google.Cloud.Firestore;
using WebAPI.Models;

namespace Infrastructure.Persistence
{
    public class DispositivoRepositoryFirestore : IDispositivoRepository
    {
        private readonly FirestoreDb _firestoreDb;

        public DispositivoRepositoryFirestore(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task GuardarAsync(DispositivoTTSDto dispositivo)
        {
            await _firestoreDb.Collection("dispositivos")
                .Document(dispositivo.DeviceId)
                .SetAsync(dispositivo);
        }

        public async Task<List<DispositivoTTSDto>> ListarAsync()
        {
            var snapshot = await _firestoreDb.Collection("dispositivos").GetSnapshotAsync();
            var dispositivos = new List<DispositivoTTSDto>();
            foreach (var doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    var dispositivo = doc.ConvertTo<DispositivoTTSDto>();
                    dispositivos.Add(dispositivo);
                }
            }
            return dispositivos;
        }
    }
}