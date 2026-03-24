using Domain.Interfaces;
using FirebaseAdmin.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Communication
{
    public class FCMService : IFCMService
    {
        private readonly ILogger<FCMService> _logger;

        public FCMService(ILogger<FCMService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> EnviarNotificacionAsync(string token, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token FCM vacío, no se puede enviar notificación");
                    return false;
                }

                var message = new Message()
                {
                    Token = token,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig()
                    {
                        Notification = new AndroidNotification()
                        {
                            Icon = "ic_notification",
                            Color = "#FF0000", // Rojo para alertas
                            DefaultSound = true
                        },
                        Priority = Priority.High
                    }
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation($"✅ Notificación FCM enviada exitosamente. Response: {response}");
                return true;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError($"❌ Error FCM: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error enviando notificación FCM: {ex.Message}");
                return false;
            }
        }

        public async Task<int> EnviarNotificacionMultipleAsync(IEnumerable<string> tokens, string title, string body, Dictionary<string, string>? data = null)
        {
            var tokensLista = tokens?.Where(t => !string.IsNullOrEmpty(t)).ToList();
            if (tokensLista == null || !tokensLista.Any())
            {
                _logger.LogWarning("No hay tokens FCM válidos para enviar notificaciones");
                return 0;
            }

            var message = new MulticastMessage()
            {
                Tokens = tokensLista,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data,
                Android = new AndroidConfig()
                {
                    Notification = new AndroidNotification()
                    {
                        Icon = "ic_notification",
                        Color = "#FF0000",
                        DefaultSound = true
                    },
                    Priority = Priority.High
                }
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation($"✅ Notificaciones FCM: {response.SuccessCount}/{tokensLista.Count} enviadas exitosamente");

                // Log de errores si los hay
                if (response.FailureCount > 0)
                {
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        if (!response.Responses[i].IsSuccess)
                        {
                            _logger.LogWarning($"❌ Error en token {i}: {response.Responses[i].Exception?.Message}");
                        }
                    }
                }

                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error enviando notificaciones FCM múltiples: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> EnviarNotificacionAlertaAsync(object alertaData)
        {
            try
            {
                _logger.LogInformation("🚨 Preparando notificación de alerta...");

                // Datos para serializar en la notificación
                var data = new Dictionary<string, string>
                {
                    { "type", "emergency_alert" },
                    { "timestamp", DateTime.UtcNow.ToString("O") },
                    { "alertData", System.Text.Json.JsonSerializer.Serialize(alertaData) }
                };

                // Este método se puede usar desde el controller específicamente
                _logger.LogInformation("📱 Notificación de alerta preparada");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error preparando notificación de alerta: {ex.Message}");
                return false;
            }
        }
    }
}