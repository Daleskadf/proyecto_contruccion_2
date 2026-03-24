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

        public async Task<string?> GetRoleByUidAsync(string uid)
        {
            var docRef = _firestoreDb.Collection("users").Document(uid);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
                return null;
            return snapshot.GetValue<string>("role");
        }

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

        // 🆕 Métodos para registro de patrulleros/operadores
        public async Task<string> RegistrarUsuarioAsync(Usuario usuario)
        {
            Console.WriteLine($"[RegistrarUsuarioAsync] Registrando usuario: {usuario.Email} con role: {usuario.Role}");

            // Crear documento en Firestore con el UID como ID del documento
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

        public async Task<Usuario?> BuscarUsuarioPorEmailAsync(string email)
        {
            Console.WriteLine($"[BuscarUsuarioPorEmailAsync] Buscando usuario por email: {email}");
            var query = _firestoreDb.Collection("users").WhereEqualTo("email", email);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                var doc = snapshot.Documents.First();
                var data = doc.ToDictionary();
                return MapearDocumentoAUsuario(doc.Id, data);
            }

            return null;
        }

        public async Task<Usuario?> BuscarUsuarioPorDniAsync(string dni)
        {
            Console.WriteLine($"[BuscarUsuarioPorDniAsync] Buscando usuario por DNI: {dni}");
            var query = _firestoreDb.Collection("users").WhereEqualTo("dni", dni);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                var doc = snapshot.Documents.First();
                var data = doc.ToDictionary();
                return MapearDocumentoAUsuario(doc.Id, data);
            }

            return null;
        }

        public async Task<List<Usuario>> ListarUsuariosPorRoleAsync(string role)
        {
            Console.WriteLine($"[ListarUsuariosPorRoleAsync] Listando usuarios por role: {role}");
            var query = _firestoreDb.Collection("users").WhereEqualTo("role", role);
            var snapshot = await query.GetSnapshotAsync();

            var usuarios = new List<Usuario>();
            foreach (var doc in snapshot.Documents)
            {
                var data = doc.ToDictionary();
                var usuario = MapearDocumentoAUsuario(doc.Id, data);
                if (usuario != null)
                {
                    usuarios.Add(usuario);
                }
            }

            return usuarios;
        }

        // Método auxiliar para mapear documentos de Firestore a objetos Usuario
        private Usuario? MapearDocumentoAUsuario(string uid, IDictionary<string, object> data)
        {
            try
            {
                string email = data.ContainsKey("email") ? data["email"]?.ToString() ?? "" : "";
                string dni = data.ContainsKey("dni") ? data["dni"]?.ToString() ?? "" : "";
                string nombre = data.ContainsKey("nombre") ? data["nombre"]?.ToString() ?? "" : "";
                string apellido = data.ContainsKey("apellido") ? data["apellido"]?.ToString() ?? "" : "";
                string role = data.ContainsKey("role") ? data["role"]?.ToString() ?? "" : "";
                string estado = data.ContainsKey("estado") ? data["estado"]?.ToString() ?? "activo" : "activo";
                bool emailVerified = data.ContainsKey("emailVerified") && Convert.ToBoolean(data["emailVerified"]);

                // Nuevos campos para FCM
                string? fcmToken = data.ContainsKey("fcmToken") ? data["fcmToken"]?.ToString() : null;

                DateTime fechaRegistro = DateTime.MinValue;
                if (data.ContainsKey("fechaRegistro") && data["fechaRegistro"] is Google.Cloud.Firestore.Timestamp ts)
                {
                    fechaRegistro = ts.ToDateTime();
                }

                DateTime? ultimaConexion = null;
                if (data.ContainsKey("ultimaConexion") && data["ultimaConexion"] is Google.Cloud.Firestore.Timestamp ucTs)
                {
                    ultimaConexion = ucTs.ToDateTime();
                }

                return new Usuario(uid, email, dni, nombre, apellido, role, fechaRegistro, emailVerified, estado, fcmToken, ultimaConexion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapearDocumentoAUsuario] Error al mapear documento: {ex.Message}");
                return null;
            }
        }

        // 🆕 Listar usuarios con filtros opcionales
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
                    var usuario = MapearDocumentoAUsuario(document.Id, document.ToDictionary());
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

        // 🆕 Editar usuario existente
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

                // Incluir FCM token y última conexión si están disponibles
                if (!string.IsNullOrEmpty(usuario.FcmToken))
                    data["fcmToken"] = usuario.FcmToken;
                if (usuario.UltimaConexion.HasValue)
                    data["ultimaConexion"] = usuario.UltimaConexion.Value;

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

        // 🆕 ===== MÉTODOS FCM =====

        /// <summary>
        /// Actualiza el token FCM de un usuario
        /// </summary>
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

        /// <summary>
        /// Obtiene tokens FCM de usuarios por role (ej: "patrullero")
        /// FIXED: Usa misma estrategia que diagnóstico para evitar problema de índice compuesto
        /// </summary>
        public async Task<List<string>> ObtenerTokensFcmPorRoleAsync(string role, bool soloActivos = true)
        {
            try
            {
                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Iniciando consulta para role: '{role}', soloActivos: {soloActivos}");

                // 🔧 SOLUCIÓN: Usar ListarUsuariosAsync() y filtrar en memoria como el diagnóstico
                var todosUsuarios = await ListarUsuariosAsync(limite: 500);
                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Total usuarios obtenidos: {todosUsuarios.Count}");

                // Filtrar por role
                var usuariosPorRole = todosUsuarios.Where(u =>
                    u.Role?.ToLower() == role?.ToLower()).ToList();
                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Usuarios con role '{role}': {usuariosPorRole.Count}");

                // Filtrar por estado activo si se requiere
                var usuariosFiltrados = soloActivos
                    ? usuariosPorRole.Where(u => u.Estado == "activo").ToList()
                    : usuariosPorRole;
                Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Usuarios filtrados (activos={soloActivos}): {usuariosFiltrados.Count}");

                var tokens = new List<string>();

                foreach (var usuario in usuariosFiltrados)
                {
                    Console.WriteLine($"[ObtenerTokensFcmPorRoleAsync] Procesando usuario: {usuario.Uid} - {usuario.Nombre}");
                    Console.WriteLine($"  - Role: '{usuario.Role}'");
                    Console.WriteLine($"  - Estado: '{usuario.Estado}'");
                    Console.WriteLine($"  - FCM Token presente: {!string.IsNullOrEmpty(usuario.FcmToken)}");

                    if (!string.IsNullOrEmpty(usuario.FcmToken))
                    {
                        Console.WriteLine($"  - Token FCM válido encontrado: {usuario.FcmToken.Substring(0, Math.Min(20, usuario.FcmToken.Length))}...");

                        // Verificar si está conectado recientemente (opcional)
                        if (usuario.UltimaConexion.HasValue)
                        {
                            var ultimaConexion = usuario.UltimaConexion.Value;
                            var minutosDesdeUltimaConexion = (DateTime.UtcNow - ultimaConexion).TotalMinutes;
                            Console.WriteLine($"  - Última conexión: {ultimaConexion:yyyy-MM-dd HH:mm:ss} (hace {minutosDesdeUltimaConexion:F1} minutos)");

                            // Solo incluir tokens de usuarios conectados en las últimas 24 horas
                            if (minutosDesdeUltimaConexion <= 1440) // 24 horas
                            {
                                tokens.Add(usuario.FcmToken);
                                Console.WriteLine($"  - ✅ Token incluido (conexión reciente)");
                            }
                            else
                            {
                                Console.WriteLine($"  - ❌ Token excluido (conexión antigua: {minutosDesdeUltimaConexion:F1} minutos)");
                            }
                        }
                        else
                        {
                            // Si no hay timestamp de última conexión, incluir el token
                            tokens.Add(usuario.FcmToken);
                            Console.WriteLine($"  - ✅ Token incluido (sin timestamp de conexión)");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  - ❌ Token FCM no válido o faltante");
                    }
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

        /// <summary>
        /// Actualiza la última conexión de un usuario
        /// </summary>
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

        /// <summary>
        /// Obtiene usuarios que tienen FCM token activo
        /// </summary>
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
                    var usuario = MapearDocumentoAUsuario(document.Id, document.ToDictionary());
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