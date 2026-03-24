# Script para generar datos de prueba en OpenData
# Ejecutar con: .\generar_datos_prueba.ps1

$BASE_URL = "http://localhost:5000"

Write-Host "🔄 Generando datos de prueba para OpenData..." -ForegroundColor Cyan

# Distritos de Tacna
$distritos = @("Tacna", "Gregorio Albarracín Lanchipa", "Ciudad Nueva", "Pocollay", "Alto de la Alianza")
$tiposViolencia = @("Física", "Psicológica", "Sexual", "Económica")
$nivelesRiesgo = @("Bajo", "Medio", "Alto", "Crítico")
$rangosEdad = @("18-29", "30-44", "45-59", "60+")

# Generar 50 atestados de prueba para los últimos 3 meses
$mesesAtras = 3
$atestadosGenerados = 0

for ($mesOffset = 0; $mesOffset -lt $mesesAtras; $mesOffset++) {
    $fecha = (Get-Date).AddMonths(-$mesOffset)
    $anio = $fecha.Year
    $mes = $fecha.Month
    
    Write-Host "`n📅 Generando datos para: $anio-$($mes.ToString('00'))" -ForegroundColor Yellow
    
    # Generar 15-20 atestados por mes
    $cantidadMes = Get-Random -Minimum 15 -Maximum 21
    
    for ($i = 0; $i -lt $cantidadMes; $i++) {
        $distrito = $distritos | Get-Random
        $lat = -18.0 + (Get-Random -Minimum -100 -Maximum 100) / 1000.0
        $lon = -70.2 + (Get-Random -Minimum -100 -Maximum 100) / 1000.0
        
        $atestado = @{
            fechaHora = (Get-Date -Year $anio -Month $mes -Day (Get-Random -Minimum 1 -Maximum 28) -Hour (Get-Random -Minimum 0 -Maximum 24) -Minute (Get-Random -Minimum 0 -Maximum 60)).ToString("yyyy-MM-ddTHH:mm:ssZ")
            latitud = [math]::Round($lat, 6)
            longitud = [math]::Round($lon, 6)
            distrito = $distrito
            tipoViolencia = $tiposViolencia | Get-Random
            nivelRiesgo = $nivelesRiesgo | Get-Random
            alertaVeridica = (Get-Random -Minimum 0 -Maximum 100) -gt 20  # 80% verídicas
            edadVictima = Get-Random -Minimum 18 -Maximum 75
            requirioAmbulancia = (Get-Random -Minimum 0 -Maximum 100) -lt 30  # 30%
            requirioRefuerzo = (Get-Random -Minimum 0 -Maximum 100) -lt 40   # 40%
            descripcionSuceso = "Incidente de prueba generado automáticamente"
            oficialACargo = "Oficial Demo"
        } | ConvertTo-Json

        try {
            $response = Invoke-RestMethod -Uri "$BASE_URL/api/atestadopolicial" -Method Post -Body $atestado -ContentType "application/json"
            $atestadosGenerados++
            Write-Host "  ✅ Atestado $atestadosGenerados creado en $distrito" -ForegroundColor Green
        }
        catch {
            Write-Host "  ❌ Error al crear atestado: $_" -ForegroundColor Red
        }
        
        Start-Sleep -Milliseconds 100  # Pequeña pausa
    }
}

Write-Host "`n✅ Total de atestados generados: $atestadosGenerados" -ForegroundColor Green

# Ahora regenerar agregados para cada mes
Write-Host "`n🔄 Regenerando agregados de OpenData..." -ForegroundColor Cyan

for ($mesOffset = 0; $mesOffset -lt $mesesAtras; $mesOffset++) {
    $fecha = (Get-Date).AddMonths(-$mesOffset)
    $anio = $fecha.Year
    $mes = $fecha.Month
    
    try {
        $response = Invoke-RestMethod -Uri "$BASE_URL/api/opendata/regenerar?anio=$anio&mes=$mes" -Method Post
        Write-Host "  ✅ Agregados regenerados para $anio-$($mes.ToString('00'))" -ForegroundColor Green
    }
    catch {
        Write-Host "  ❌ Error al regenerar agregados: $_" -ForegroundColor Red
    }
}

Write-Host "`n🎉 ¡PROCESO COMPLETADO!" -ForegroundColor Magenta
Write-Host "Ahora puedes acceder a:" -ForegroundColor White
Write-Host "  - Dashboard: $BASE_URL/api/opendata/dashboard" -ForegroundColor Cyan
Write-Host "  - CSV: $BASE_URL/api/opendata/descargar/csv?anio=$((Get-Date).Year)&mes=$((Get-Date).Month)" -ForegroundColor Cyan
Write-Host "  - JSON: $BASE_URL/api/opendata/descargar/json?anio=$((Get-Date).Year)" -ForegroundColor Cyan
