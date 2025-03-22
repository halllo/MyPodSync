# Get podman ready
podman machine start

# Run the container for 10 minutes (600 seconds) and then stop it
Start-Process -NoNewWindow -FilePath podman -ArgumentList @(
    "run",
    "-p", "5055:8080",
    "--rm",
    "-it",
    "-v", "D:\MyPodSync\acishare:/app/data/",
    "-e", "PODSYNC_CONFIG_PATH=/app/data/config.toml",
    "ghcr.io/mxpv/podsync:latest"
)
Start-Sleep -Seconds 600
podman stop $(podman ps -q --filter ancestor=ghcr.io/mxpv/podsync:latest)

# Sync to the cloud
$CurrentDir = Get-Location
cd D:\MyPodSync\MyPodSync\MyPodSync.Sync\bin\Debug\net8.0
.\MyPodSync.Sync.exe sync --source "D:\MyPodSync\acishare\files"
cd $CurrentDir