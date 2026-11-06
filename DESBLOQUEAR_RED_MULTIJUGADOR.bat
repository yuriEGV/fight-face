@echo off
chcp 65001 >nul
title Desbloquear Red y Firewall - Fight Face

echo ========================================================
echo   FIGHT FACE - CONFIGURACION DE RED LOCAL Y FIREWALL
echo ========================================================
echo.
echo Verificando permisos de Administrador...

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo [!] Solicitando permisos de Administrador de Windows...
    echo (Por favor haz clic en "SI" en la ventana de Windows que aparecera)
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo.
echo [+] Permisos concedidos.
echo [+] 1. Configurando perfil de red de Windows a PRIVADA (para permitir juego en LAN)...
powershell -NoProfile -Command "Get-NetConnectionProfile | ForEach-Object { Set-NetConnectionProfile -InterfaceIndex $_.InterfaceIndex -NetworkCategory Private -ErrorAction SilentlyContinue }"

echo [+] 2. Eliminando bloqueos de Firewall para Fight Face y Unity...
powershell -NoProfile -Command "Get-NetFirewallRule | Where-Object { ($_.DisplayName -like '*Unity*' -or $_.DisplayName -like '*fightface*') -and $_.Action -eq 'Block' } | Remove-NetFirewallRule -ErrorAction SilentlyContinue"
powershell -NoProfile -Command "Get-NetFirewallApplicationFilter -Program '*FightFace.exe*' -ErrorAction SilentlyContinue | Get-NetFirewallRule | Where-Object { $_.Action -eq 'Block' } | Remove-NetFirewallRule -ErrorAction SilentlyContinue"

echo [+] 3. Habilitando puertos 7777 y 7778 (UDP y TCP) para Fight Face...
powershell -NoProfile -Command "Remove-NetFirewallRule -DisplayName 'FightFace_UDP_LAN' -ErrorAction SilentlyContinue; New-NetFirewallRule -DisplayName 'FightFace_UDP_LAN' -Direction Inbound -Protocol UDP -LocalPort 7777,7778 -Action Allow -Profile Any -ErrorAction SilentlyContinue"
powershell -NoProfile -Command "Remove-NetFirewallRule -DisplayName 'FightFace_TCP_LAN' -ErrorAction SilentlyContinue; New-NetFirewallRule -DisplayName 'FightFace_TCP_LAN' -Direction Inbound -Protocol TCP -LocalPort 7777,7778 -Action Allow -Profile Any -ErrorAction SilentlyContinue"

echo [+] 4. Habilitando ejecutable FightFace en el Firewall...
powershell -NoProfile -Command "Remove-NetFirewallRule -DisplayName 'FightFace_Exe_Allow' -ErrorAction SilentlyContinue; New-NetFirewallRule -DisplayName 'FightFace_Exe_Allow' -Direction Inbound -Program '%~dp0FightFace.exe' -Action Allow -Profile Any -ErrorAction SilentlyContinue"

echo.
echo ========================================================
echo   LISTO! Red y Firewall configurados exitosamente.
echo   Ahora puedes conectar tu PC con celulares por Wi-Fi.
echo ========================================================
echo.
pause
