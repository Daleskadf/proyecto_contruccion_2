using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Domain.Interfaces;
using WebAPI.Models;


namespace Infrastructure.Communication
{
    public class TTSDeviceService : ITTSDeviceService
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _appId;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TTSDeviceService> _logger;

        public TTSDeviceService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<TTSDeviceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiKey = configuration["TTS:ApiKey"] ?? throw new ArgumentNullException("TTS:ApiKey");
            _baseUrl = configuration["TTS:BaseUrl"] ?? throw new ArgumentNullException("TTS:BaseUrl");
            _appId = configuration["TTS:AppId"] ?? throw new ArgumentNullException("TTS:AppId");
        }

        public async Task RegistrarDispositivoAsync(string deviceId, string devEui, string joinEui, string appKey)
        {
            // 1. Network Server (NS)
            var nsPayload = new
            {
                end_device = new
                {
                    ids = new { device_id = deviceId, dev_eui = devEui, join_eui = joinEui },
                    lorawan_version = "MAC_V1_0_4",
                    lorawan_phy_version = "PHY_V1_0_3_REV_A",
                    frequency_plan_id = "AU_915_928_FSB_2",
                    supports_join = true
                },
                field_mask = new
                {
                    paths = new[]
                    {
                        "supports_join",
                        "lorawan_version",
                        "lorawan_phy_version",
                        "frequency_plan_id",
                        "ids.dev_eui",
                        "ids.join_eui",
                    }
                }
            };
            await PostDevice($"{_baseUrl}/ns/applications/{_appId}/devices", nsPayload, "NS");

            // 2. Application Server (AS)
            var asPayload = new
            {
                end_device = new
                {
                    ids = new { device_id = deviceId, dev_eui = devEui }
                },
                field_mask = new { paths = new string[] { } }
            };
            await PostDevice($"{_baseUrl}/as/applications/{_appId}/devices", asPayload, "AS");

            // 3. Join Server (JS)
            var jsPayload = new
            {
                end_device = new
                {
                    ids = new { device_id = deviceId, dev_eui = devEui, join_eui = joinEui },
                    network_server_address = "nam1.cloud.thethings.network",
                    application_server_address = "nam1.cloud.thethings.network",
                    root_keys = new { app_key = new { key = appKey } }
                },
                field_mask = new
                {
                    paths = new[]
                    {
                        "network_server_address",
                        "application_server_address",
                        "ids.dev_eui",
                        "ids.join_eui",
                        "root_keys.app_key.key"
                    }
                }
            };
            await PostDevice($"{_baseUrl}/js/applications/{_appId}/devices", jsPayload, "JS");
        }

        private async Task PostDevice(string url, object payload, string serverName)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            client.DefaultRequestHeaders.Add("User-Agent", "SISALERTLORA/1.0");

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation($"URL: {url}");
            _logger.LogInformation($"Payload enviado a {serverName}:\n{json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Status Code {serverName}: {response.StatusCode}");
            _logger.LogInformation($"Response {serverName}:\n{responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"ERROR en {serverName}: {responseBody}");
                throw new Exception($"Error al registrar en {serverName} ({response.StatusCode}): {responseBody}");
            }
            _logger.LogInformation($"✓ {serverName} - Registro exitoso");
        }
        public async Task<List<DispositivoTTSDto>> ListarDispositivosAsync()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var url = $"{_baseUrl}/as/applications/{_appId}/devices";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            // El resultado será un objeto con un array "end_devices"
            using var doc = JsonDocument.Parse(json);
            var dispositivos = new List<DispositivoTTSDto>();
            if (doc.RootElement.TryGetProperty("end_devices", out var devicesArray))
            {
                foreach (var dev in devicesArray.EnumerateArray())
                {
                    dispositivos.Add(new DispositivoTTSDto
                    {
                        DeviceId = dev.GetProperty("ids").GetProperty("device_id").GetString() ?? "",
                        DevEui = dev.GetProperty("ids").GetProperty("dev_eui").GetString() ?? ""
                    });
                }
            }
            return dispositivos;
        }
    }
}