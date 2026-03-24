using Google.Cloud.Firestore;
using System.Threading.Tasks;
using Domain.Interfaces;
using WebAPI.Models;
using System.Collections.Generic;
using Domain.Entities;

namespace Infrastructure.Persistence
{
    public class UserRepositoryFirestore : IUserRepositoryFirestore
    {
        private readonly FirestoreDb _firestoreDb;

        public UserRepositoryFirestore(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<UsuarioDto?> BuscarPorDniAsync(string dni)
        {
            Console.WriteLine($"[BuscarPorDniAsync] Buscando en Firestore por DNI: [{dni}]");
            var query = _firestoreDb.Collection("users").WhereEqualTo("dni", dni);
            var snapshot = await query.GetSnapshotAsync();
            Console.WriteLine($"[BuscarPorDniAsync] Cantidad de documentos encontrados: {snapshot.Documents.Count}");
            foreach (var doc in snapshot.Documents)
            {
                Console.WriteLine($"[BuscarPorDniAsync] Documento encontrado: {doc.Id}");
                return doc.ConvertTo<UsuarioDto>();
            }
            Console.WriteLine("[BuscarPorDniAsync] No se encontró ningún usuario con ese DNI.");
            return null;
        }

        public async Task<UsuarioDto?> BuscarPorUidAsync(string uid)
        {
            var docRef = _firestoreDb.Collection("users").Document(uid);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
                return null;
            return snapshot.ConvertTo<UsuarioDto>();
        }


        // Busca usuarios por rol
        // Lista de usuarios con ese role
        public async Task<List<UsuarioDto>> BuscarPorRolAsync(string rol)
        {
            var query = _firestoreDb.Collection("users").WhereEqualTo("role", rol);
            var snapshot = await query.GetSnapshotAsync();
            var usuarios = new List<UsuarioDto>();
            foreach (var doc in snapshot.Documents)
            {
                usuarios.Add(doc.ConvertTo<UsuarioDto>());
            }
            return usuarios;
        }

        // Obtiene el role de un usuario por su UID
        // Role del usuario o null si no existe
        public async Task<string?> GetRoleByUidAsync(string uid)
        {
            var docRef = _firestoreDb.Collection("users").Document(uid);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
                return null;
            return snapshot.GetValue<string>("role");
        }

        // Vincula un dispositivo LoRaWAN a un usuario por su DNI
        // DTO con DNI y device_id
        public async Task VincularDispositivoAsync(VincularDispositivoDto vincularDto)
        {
            var dni = vincularDto.Dni;
            var deviceId = vincularDto.DeviceId;

            var query = _firestoreDb.Collection("users").WhereEqualTo("dni", dni);
            var snapshot = await query.GetSnapshotAsync();
            foreach (var doc in snapshot.Documents)
            {
                await doc.Reference.UpdateAsync(new Dictionary<string, object>
                {
                    { "device_id", deviceId }
                });
                return;
            }
            throw new Exception("Usuario no encontrado para vincular.");
        }

        // Busca un usuario por su device_id (dispositivo LoRaWAN)
        public async Task<UsuarioDto?> BuscarPorDeviceIdAsync(string deviceId)
        {
            var query = _firestoreDb.Collection("users").WhereEqualTo("device_id", deviceId);
            var snapshot = await query.GetSnapshotAsync();
            foreach (var doc in snapshot.Documents)
            {
                return doc.ConvertTo<UsuarioDto>();
            }
            return null;
        }

        /// <summary>
        /// Registra un nuevo usuario en Firestore con el UID como ID del documento
        /// </summary>
        /// <param name="usuario">Usuario a registrar</param>
        /// <returns>UID del usuario registrado</returns>
        public async Task<string> RegistrarUsuarioAsync(Usuario usuario)
        {
            Console.WriteLine($"[RegistrarUsuarioAsync] Registrando usuario: {usuario.Email} con role: {usuario.Role}");

            var docRef = _firestoreDb.Collection("users").Document(usuario.Uid);

            await docRef.SetAsync(new
            {
                email = usuario.Email,
                dni = usuario.Dni,
                nombre = usuario.Nombre,
                role = usuario.Role,
                fechaRegistro = usuario.FechaRegistro,
                emailVerified = usuario.EmailVerified,
                estado = usuario.Estado
            });

            Console.WriteLine($"[RegistrarUsuarioAsync] Usuario registrado exitosamente con UID: {usuario.Uid}");
            return usuario.Uid;
        }

        // Busca un usuario por su email
        // Usuario encontrado o null si no existe
        public async Task<Usuario?> BuscarUsuarioPorEmailAsync(string email)
        {
            Console.WriteLine($"[BuscarUsuarioPorEmailAsync] Buscando usuario por email: {email}");
            var query = _firestoreDb.Collection("users").WhereEqualTo("email", email);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                var doc = snapshot.Documents.First();
                var data = doc.ToDictionary();
                return this.MapearDocumentoAUsuario(doc.Id, data);
            }

            return null;
        }

        // Busca un usuario por su DNI
        public async Task<Usuario?> BuscarUsuarioPorDniAsync(string dni)
        {
            Console.WriteLine($"[BuscarUsuarioPorDniAsync] Buscando usuario por DNI: {dni}");
            var query = _firestoreDb.Collection("users").WhereEqualTo("dni", dni);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                var doc = snapshot.Documents.First();
                var data = doc.ToDictionary();
                return this.MapearDocumentoAUsuario(doc.Id, data);
            }

            return null;
        }

        // Lista usuarios por role específico
        public async Task<List<Usuario>> ListarUsuariosPorRoleAsync(string role)
        {
            Console.WriteLine($"[ListarUsuariosPorRoleAsync] Listando usuarios por role: {role}");
            var query = _firestoreDb.Collection("users").WhereEqualTo("role", role);
            var snapshot = await query.GetSnapshotAsync();

            var usuarios = new List<Usuario>();
            foreach (var doc in snapshot.Documents)
            {
                var data = doc.ToDictionary();
                var usuario = this.MapearDocumentoAUsuario(doc.Id, data);
                if (usuario != null)
                {
                    usuarios.Add(usuario);
                }
            }

            return usuarios;
        }

        private Usuario? MapearDocumentoAUsuario(string uid, IDictionary<string, object> data)
        {
            try
            {
                string email = this.ObtenerValorDiccionario(data, "email");
                string dni = this.ObtenerValorDiccionario(data, "dni");
                string nombre = this.ObtenerValorDiccionario(data, "nombre");
                string apellido = this.ObtenerValorDiccionario(data, "apellido");
                string role = this.ObtenerValorDiccionario(data, "role");
                string estado = this.ObtenerValorDiccionario(data, "estado", "activo");
                bool emailVerified = data.ContainsKey("emailVerified") && Convert.ToBoolean(data["emailVerified"]);

                // Campos para FCM y auditoría
                string? fcmToken = data.ContainsKey("fcmToken") ? data["fcmToken"]?.ToString() : null;

                DateTime fechaRegistro = this.ObtenerTimestampDiccionario(data, "fechaRegistro");
                DateTime? ultimaConexion = this.ObtenerTimestampNulableDiccionario(data, "ultimaConexion");

                return new Usuario(uid, email, dni, nombre, apellido, role, fechaRegistro, emailVerified, estado, fcmToken, ultimaConexion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapearDocumentoAUsuario] Error al mapear documento: {ex.Message}");
                return null;
            }
        }

        // Obtiene un valor string del diccionario con valor por defecto
        private string ObtenerValorDiccionario(IDictionary<string, object> data, string clave, string valorPorDefecto = "")
        {
            return data.ContainsKey(clave) ? data[clave]?.ToString() ?? valorPorDefecto : valorPorDefecto;
        }

        /// <summary>
        /// Obtiene un valor Timestamp del diccionario
        /// </summary>
        private DateTime ObtenerTimestampDiccionario(IDictionary<string, object> data, string clave)
        {
            if (data.ContainsKey(clave) && data[clave] is Google.Cloud.Firestore.Timestamp ts)
            {
                return ts.ToDateTime();
            }
            return DateTime.MinValue;
        }

        // Obtiene un valor Timestamp nullable del diccionario
        private DateTime? ObtenerTimestampNulableDiccionario(IDictionary<string, object> data, string clave)
        {
            if (data.ContainsKey(clave) && data[clave] is Google.Cloud.Firestore.Timestamp ts)
            {
                return ts.ToDateTime();
            }
            return null;
        }

        // Lista usuarios con filtros opcionales
        public async Task<List<Usuario>> ListarUsuariosAsync(int limite = 50, string? filtroRole = null)
        {
            try
            {
                Query query = _firestoreDb.Collection("users");

                // Aplicar filtro por role si se proporciona
                if (!string.IsNullOrEmpty(filtroRole))
                {
                    query = query.WhereEqualTo("role", filtroRole);
                }

                // Aplicar límite
                query = query.Limit(limite);

                var snapshot = await query.GetSnapshotAsync();
                var usuarios = new List<Usuario>();

                foreach (var document in snapshot.Documents)
                {
                    var usuario = this.MapearDocumentoAUsuario(document.Id, document.ToDictionary());
                    if (usuario != null)
                    {
                        usuarios.Add(usuario);
                    }
                }

                Console.WriteLine($"[ListarUsuariosAsync] Encontrados {usuarios.Count} usuarios");
                return usuarios;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ListarUsuariosAsync] Error: {ex.Message}");
                return new List<Usuario>();
            }
        }

        // Edita un usuario existente en Firestore
        public async Task<bool> EditarUsuarioAsync(Usuario usuario)
        {
            try
            {
                var docRef = _firestoreDb.Collection("users").Document(usuario.Uid);

                var data = new Dictionary<string, object>
                {
                    ["email"] = usuario.Email,
                    ["dni"] = usuario.Dni,
                    ["nombre"] = usuario.Nombre,
                    ["apellido"] = usuario.Apellido ?? string.Empty,
                    ["role"] = usuario.Role,
                    ["estado"] = usuario.Estado,
                    ["emailVerified"] = usuario.EmailVerified,
                    ["fechaRegistro"] = usuario.FechaRegistro
                };

                // Agregar campos opcionales si están disponibles
                if (!string.IsNullOrEmpty(usuario.FcmToken))
                {
                    data["fcmToken"] = usuario.FcmToken;
                }

                if (usuario.UltimaConexion.HasValue)
                {
                    data["ultimaConexion"] = usuario.UltimaConexion.Value;
                }

                await docRef.UpdateAsync(data);
                Console.WriteLine($"[EditarUsuarioAsync] Usuario actualizado: {usuario.Uid}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EditarUsuarioAsync] Error: {ex.Message}");
                return false;
            }
        }

        // ===== MÉTODOS FCM =====

        // Actualiza el token FCM de un usuario y registra la última conexión
        public async Task<bool> ActualizarFcmTokenAsync(string uid, string fcmToken)
        {
            try
            {
                var docRef = _firestoreDb.Collection("users").Document(uid);

                var updates = new Dictionary<string, object>
                {
                    ["fcmToken"] = fcmToken,
                    ["ultimaConexion"] = DateTime.UtcNow
                };

                await docRef.UpdateAsync(updates);
                Console.WriteLine($"[ActualizarFcmTokenAsync] Token FCM actualizado para usuario: {uid}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ActualizarFcmTokenAsync] Error: {ex.Message}");
                return false;
            }
        }

        // Obtiene tokens FCM de usuarios por role (ej: "patrullero")
        // Filtra en memoria para evitar limitaciones de índices compuestos en Firestore
        // Solo retorna tokens de usuarios activos conectados en las últimas 24 horas
        public async Task<List<string>> ObtenerTokensFcmPorRoleAsync(string role, bool soloActivos = true)
        {
            const int limite = 500;
            const int minutosConexionActiva = 1440; // 24 horas

            try
            {
                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Iniciando consulta para role: '{role}', soloActivos: {soloActivos}");

                // Obtener todos los usuarios hasta el límite especificado
                var todosUsuarios = await this.ListarUsuariosAsync(limite: limite);
                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Total usuarios obtenidos: {todosUsuarios.Count}");

                // Filtrar por role (case-insensitive)
                var usuariosPorRole = todosUsuarios
                    .Where(u => u.Role?.ToLower() == role?.ToLower())
                    .ToList();
                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Usuarios con role '{role}': {usuariosPorRole.Count}");

                // Filtrar por estado activo si se requiere
                var usuariosFiltrados = soloActivos
                    ? usuariosPorRole.Where(u => u.Estado == "activo").ToList()
                    : usuariosPorRole;
                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Usuarios filtrados (activos={soloActivos}): {usuariosFiltrados.Count}");

                var tokens = new List<string>();

                foreach (var usuario in usuariosFiltrados)
                {
                    this.ProcesarTokenFcmDeUsuario(usuario, tokens, minutosConexionActiva);
                }

                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Encontrados {tokens.Count} tokens FCM para role: {role}");
                return tokens;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Error: {ex.Message}");
                return new List<string>();
            }
        }

        // Procesa el token FCM de un usuario y lo agrega a la lista si es válido

        private void ProcesarTokenFcmDeUsuario(Usuario usuario, List<string> tokens, int minutosConexionActiva)
        {
            Console.WriteLine($"[ProcesarTokenFcmDeUsuario] Procesando usuario: {usuario.Uid} - {usuario.Nombre}");
            Console.WriteLine($"  - Role: '{usuario.Role}'");
            Console.WriteLine($"  - Estado: '{usuario.Estado}'");
            Console.WriteLine($"  - FCM Token presente: {!string.IsNullOrEmpty(usuario.FcmToken)}");

            if (string.IsNullOrEmpty(usuario.FcmToken))
            {
                Console.WriteLine($"  - ❌ Token FCM no válido o faltante");
                return;
            }

            Console.WriteLine($"  - Token FCM válido encontrado: {usuario.FcmToken.Substring(0, Math.Min(20, usuario.FcmToken.Length))}...");

            // Si no hay timestamp de última conexión, incluir el token
            if (!usuario.UltimaConexion.HasValue)
            {
                tokens.Add(usuario.FcmToken);
                Console.WriteLine($"  - ✅ Token incluido (sin timestamp de conexión)");
                return;
            }

            // Verificar si está conectado recientemente (últimas 24 horas)
            var ultimaConexion = usuario.UltimaConexion.Value;
            var minutosDesdeUltimaConexion = (DateTime.UtcNow - ultimaConexion).TotalMinutes;
            Console.WriteLine($"  - Última conexión: {ultimaConexion:yyyy-MM-dd HH:mm:ss} (hace {minutosDesdeUltimaConexion:F1} minutos)");

            if (minutosDesdeUltimaConexion <= minutosConexionActiva)
            {
                tokens.Add(usuario.FcmToken);
                Console.WriteLine($"  - ✅ Token incluido (conexión reciente)");
            }
            else
            {
                Console.WriteLine($"  - ❌ Token excluido (conexión antigua: {minutosDesdeUltimaConexion:F1} minutos)");
            }
        }

        // Registra la última conexión de un usuario con la fecha/hora actual
        public async Task<bool> ActualizarUltimaConexionAsync(string uid)
        {
            try
            {
                var docRef = _firestoreDb.Collection("users").Document(uid);

                var updates = new Dictionary<string, object>
                {
                    ["ultimaConexion"] = DateTime.UtcNow
                };

                await docRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ActualizarUltimaConexionAsync] Error: {ex.Message}");
                return false;
            }
        }

        // Obtiene usuarios que tienen FCM token activo
        public async Task<List<Usuario>> ObtenerUsuariosConFcmActivoAsync(string? role = null)
        {
            try
            {
                Query query = _firestoreDb.Collection("users")
                    .WhereEqualTo("estado", "activo");

                if (!string.IsNullOrEmpty(role))
                {
                    query = query.WhereEqualTo("role", role);
                }

                var snapshot = await query.GetSnapshotAsync();
                var usuarios = new List<Usuario>();

                foreach (var document in snapshot.Documents)
                {
                    var usuario = this.MapearDocumentoAUsuario(document.Id, document.ToDictionary());
                    if (usuario != null && !string.IsNullOrEmpty(usuario.FcmToken))
                    {
                        usuarios.Add(usuario);
                    }
                }

                Console.WriteLine($"[ObtenerUsuariosConFcmActivoAsync] Encontrados {usuarios.Count} usuarios con FCM activo");
                return usuarios;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ObtenerUsuariosConFcmActivoAsync] Error: {ex.Message}");
                return new List<Usuario>();
            }
        }
    }
}