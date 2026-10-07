#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Publishes HW Pulse, installs the PawnIO driver and registers the logon task.

.DESCRIPTION
    Run it again to update: it stops the running instance, republishes over the same folder and
    keeps settings.json, which publishing never overwrites.

.PARAMETER Destination
    Install folder. Defaults to "C:\Program Files\HW Pulse".
#>
param(
    [string]$Destination = (Join-Path $env:ProgramFiles 'HW Pulse')
)

$ErrorActionPreference = 'Stop'

$TaskName = 'HW Pulse'
$Project = Join-Path $PSScriptRoot '..\src\HwPulse.App\HwPulse.App.csproj'
$Exe = Join-Path $Destination 'HwPulse.exe'

# El ejecutable en uso no se puede sobrescribir.
Write-Host 'Deteniendo la instancia en marcha...'
Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
Get-Process -Name HwPulse -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Publicando en $Destination..."
dotnet publish $Project -c Release -o $Destination
if ($LASTEXITCODE -ne 0) { throw "dotnet publish ha fallado (código $LASTEXITCODE)." }

# Sin PawnIO no hay temperatura de CPU ni ventiladores.
if (winget list --id namazso.PawnIO -e --accept-source-agreements | Select-String 'namazso.PawnIO') {
    Write-Host 'PawnIO ya está instalado.'
}
else {
    Write-Host 'Instalando PawnIO...'
    winget install --id namazso.PawnIO -e --silent --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) { throw "La instalación de PawnIO ha fallado (código $LASTEXITCODE)." }
}

# Al iniciar sesión el usuario actual y con privilegios más altos: así arranca sin el aviso de UAC.
# Sin límite de tiempo: por defecto Windows detiene las tareas a las 72 horas.
$user = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$action = New-ScheduledTaskAction -Execute $Exe -WorkingDirectory $Destination
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $user
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
Start-ScheduledTask -TaskName $TaskName

Write-Host "HW Pulse instalado en $Destination y en marcha. Arrancará solo al iniciar sesión."
