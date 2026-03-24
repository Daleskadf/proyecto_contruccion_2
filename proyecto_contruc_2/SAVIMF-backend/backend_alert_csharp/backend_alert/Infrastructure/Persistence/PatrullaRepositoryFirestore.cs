using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Persistence
{
    public class PatrullaRepositoryFirestore : IPatrulleroRepository
    {
        private readonly FirestoreDb _firestoreDb;

        public PatrullaRepositoryFirestore(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task SaveAsync(Patrulla patrulla)
        {
            await _firestoreDb.Collection("ubicaciones_patrullas")
                .Document(patrulla.PatrulleroId)
                .SetAsync(new
                {
                    patrulleroId = patrulla.PatrulleroId,
                    lat = patrulla.Lat,
                    lon = patrulla.Lon,
                    timestamp = patrulla.Timestamp
                }, SetOptions.MergeAll);
        }

        public async Task<List<Patrulla>> GetUltimasPatrullasAsync()
        {
            var snapshot = await _firestoreDb.Collection("ubicaciones_patrullas").GetSnapshotAsync();
            var patrullas = new List<Patrulla>();
            foreach (var doc in snapshot.Documents)
            {
                var data = doc.ToDictionary();

                // Validación previa para evitar nulos
                if (data.TryGetValue("patrulleroId", out var patrulleroIdObj) &&
                    patrulleroIdObj != null &&
                    data.TryGetValue("lat", out var latObj) &&
                    data.TryGetValue("lon", out var lonObj) &&
                    data.TryGetValue("timestamp", out var timestampObj))
                {
                    string patrulleroId = patrulleroIdObj.ToString()!;
                    double lat = Convert.ToDouble(latObj);
                    double lon = Convert.ToDouble(lonObj);

                    // 🔧 Conversión correcta de Firestore.Timestamp a DateTime
                    DateTime timestamp;
                    if (timestampObj is Timestamp firestoreTimestamp)
                    {
                        timestamp = firestoreTimestamp.ToDateTime();
                    }
                    else if (timestampObj is DateTime dateTime)
                    {
                        timestamp = dateTime;
                    }
                    else
                    {
                        // Fallback: usar tiempo actual si hay error
                        Console.WriteLine($"Error convirtiendo timestamp para patrulla {patrulleroId}. Usando tiempo actual.");
                        timestamp = DateTime.UtcNow;
                    }

                    patrullas.Add(new Patrulla(
                        patrulleroId,
                        lat,
                        lon,
                        timestamp
                    ));
                }
            }
            return patrullas;
        }
    }
}