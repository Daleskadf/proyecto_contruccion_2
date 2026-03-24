using Google.Cloud.Firestore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Domain.Interfaces;
using backend_alert.Domain.Interfaces;
using backend_alert.Domain.Entities;
using Infrastructure.Persistence;
using backend_alert.Infrastructure.Persistence;
using Infrastructure.Auth;
using Infrastructure.Services;
using Application.UseCases;
using backend_alert.Application.UseCases;
using Application.BackgroundServices;
using Infrastructure.Communication;
using WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Configurar logging específico para SignalR
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Configuración específica para categorías
builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Debug);

// === CONFIGURACIÓN FIREBASE Y FIRESTORE ===

// Ruta al archivo de credenciales (ajusta según tu entorno)
string credentialsPath = "sis-alert-firebase-admin.json";
string projectId = "sis-alert-1e7a7";

// Inicializa FirebaseApp una sola vez y regístralo en DI
var firebaseApp = FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(credentialsPath)
});
builder.Services.AddSingleton(firebaseApp);

// Registra FirestoreDb como Singleton en DI
builder.Services.AddSingleton(provider =>
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
    return FirestoreDb.Create(projectId);
});

// === REGISTRA SERVICIOS Y CASOS DE USO ===
builder.Services.AddScoped<IAlertaRepository, AlertaRepositoryFirestore>();
builder.Services.AddScoped<IPatrulleroRepository, PatrullaRepositoryFirestore>();
builder.Services.AddScoped<ValidadorDatosService>();
builder.Services.AddScoped<RegistrarAlertaUseCase>();
builder.Services.AddScoped<ActualizarUbicacionPatrullaUseCase>();
builder.Services.AddSingleton<UserRepositoryFirestore>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();
builder.Services.AddScoped<IUserRepositoryFirestore, UserRepositoryFirestore>();
builder.Services.AddScoped<RegistrarDispositivoTTSUseCase>();
builder.Services.AddScoped<ITTSDeviceService, TTSDeviceService>();
builder.Services.AddScoped<IDispositivoRepository, DispositivoRepositoryFirestore>();
builder.Services.AddScoped<BuscarUsuarioPorDniUseCase>();
builder.Services.AddScoped<VincularDispositivoUseCase>();
builder.Services.AddScoped<ListarDispositivosConVinculoUseCase>();
builder.Services.AddScoped<ListarAlertasUseCase>();
builder.Services.AddScoped<ListarUbicacionesPatrullasUseCase>();
builder.Services.AddScoped<RegistrarUsuarioUseCase>(); 
builder.Services.AddScoped<ListarUsuariosUseCase>(); 
builder.Services.AddScoped<EditarUsuarioUseCase>(); 

builder.Services.AddScoped<RegistrarTokenFcmUseCase>(); 
builder.Services.AddScoped<IFCMService, FCMService>();

builder.Services.AddScoped<IAtestadoPolicialRepository, AtestadoPolicialRepositoryFirestore>();
builder.Services.AddScoped<IOpenDataRepository, OpenDataRepositoryFirestore>();
builder.Services.AddScoped<RegistrarAtestadoPolicialUseCase>();
builder.Services.AddScoped<ObtenerOpenDataUseCase>();

// === SERVICIOS EN BACKGROUND ===
// Servicio que expira alertas automáticamente cada minuto
builder.Services.AddHostedService<AlertaExpirationService>();

// === CONFIGURACIÓN DE CORS ===
// En desarrollo: permitir cualquier origen
// En producción: usar dominio real (por ejemplo, https://sis-alert.pe)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Permite CUALQUIER origen en desarrollo
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Necesario para SignalR
    });

    // Política específica para SignalR (más permisiva)
    options.AddPolicy("SignalRCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient(); // para IHttpClientFactory

builder.Services
    .AddAuthentication(options =>
    {
        // Configurar esquema por defecto como JWT Bearer
        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Firebase Project ID
        options.Authority = $"https://securetoken.google.com/{projectId}";
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{projectId}",
            ValidateAudience = true,
            ValidAudience = projectId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5) // Tolerancia de 5 minutos
        };
        
        // Configuración adicional para debugging
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"Authentication challenge: {context.Error} - {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Configuración mejorada de SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Para debug
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60); // Aumentado
    options.HandshakeTimeout = TimeSpan.FromSeconds(30); // Aumentado
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
    options.StreamBufferCapacity = 10;
}).AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null; // Mantener nombres originales
});

var app = builder.Build();

// === CONFIGURACIÓN DEL PIPELINE ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS debe ir antes de routing y auth
app.UseCors("DefaultCorsPolicy");

app.UseRouting();
app.UseAuthentication(); // Agregar Authentication middleware
app.UseAuthorization();  // Agregar Authorization middleware

// Mapea controladores
app.MapControllers();


// Hub de SignalR con configuraciones específicas
app.MapHub<AlertaHub>("/alertaHub", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling |
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
}); // Usar la política de CORS por defecto

app.Run();
