
ACTUAL

backend_alert_csharp/ 
│   backend_alert/
│   ├── Application/
│   │   └── UseCases/
│   │       ├── ActualizarUbicacionPatrullaUseCase.cs
│   │       ├── RegistrarAlertaUseCase.cs
│   │       ├── RegistrarDispositivoTTSUseCase.cs
│   │       ├── ListarDispositivosConVinculoUseCase.cs
│   │       ├── BuscarVictimaPorDniUseCase.cs
│   │       ├── VincularDispositivoUseCase.cs
│   │       ├── ListarAlertasUseCase.cs
│   │       └── LoginUseCase.cs
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── Alerta.cs
│   │   │   ├── Ubicacion.cs
│   │   │   └── Usuario.cs
│   │   └── Interfaces/
│   │       ├── IAlertaRepository.cs
│   │       ├── IUserRepositoryFirestore.cs
│   │       ├── IFirebaseAuthService.cs
│   │       ├── ITTSDeviceService.cs
│   │       ├── IDispositivoRepository.cs
│   │       └── IPatrulleroRepository.cs
│   ├── Infrastructure/
│   │   ├── Auth/
│   │   │   └── FirebaseAuthService.cs
│   │   ├── Communication/
│   │   │   └── TTSDeviceService.cs
│   │   ├── Persistence/
│   │   │   ├── AlertaRepositoryFirestore.cs
│   │   │   ├── PatrullaRepositoryFirestore.cs
│   │   │   ├── DispositivoRepositoryFirestore.cs
│   │   │   └── UserRepositoryFirestore.cs
│   │   └── Services/
│   │       └── ValidadorDatosService.cs
│   ├── WebAPI/
│   │   ├── Controllers/
│   │   │   ├── AlertaController.cs
│   │   │   ├── PatrullaController.cs
│   │   │   ├── DeviceController.cs
│   │   │   ├── UserController.cs
│   │   │   ├── AuthController.cs
│   │   │   └── Models/
│   │   │       ├── LoginFirebaseRequestDto.cs
│   │   │       ├── UbicacionPatrullaDto.cs
│   │   │       ├── RegistrarDispositivoDto.cs
│   │   │       ├── DispositivoTTSDto.cs
│   │   │       ├── UsuarioDto.cs
│   │   │       ├── VincularDispositivoDto.cs
│   │   │       ├── DispositivoListadoDto.cs
│   │   │       └── UsuarioFirebaseDto.CS
│   │   ├── Dockerfile
│   │   ├── Filters/
│   │   │   └── FirebaseAuthGuardAttribute.cs
│   │   └── Hubs/
│   │       └── AlertaHub.cs
│   ├── Program.cs
│   └── backend_alert_csharp.csproj
│
├── backend_alert_csharp.Test/
│
├── docker-compose.yml
├── backend_alert_csharp.sln
└── README.MD


7b22475053223a222d31382e303435333337362c2d37302e323534353932222c2242617474657279223a37387d 
7b22475053223a222d31382e303437343333382c2d37302e32363332393531222c2242617474657279223a37387d

7b22475053223a222d31382e3030313335393437313238313334372c2d37302e32343939303737303836353532222c2242617474657279223a37387d





7b22475053223a222d31382e3031303332382c2d37302e323533353937222c2242617474657279223a37387d
