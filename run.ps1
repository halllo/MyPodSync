# Get podman ready
podman machine start

# Run the container for 30 minutes (1800 seconds) and then stop it
Start-Job -ScriptBlock {
    podman run -p 5055:8080 --rm -it -v D:\MyPodSync\acishare:/app/data/ -e "PODSYNC_CONFIG_PATH=/app/data/config.toml" ghcr.io/mxpv/podsync:latest
} | Out-Null

Start-Sleep -Seconds 1800
podman stop $(podman ps -q --filter ancestor=ghcr.io/mxpv/podsync:latest)

# Sync to the cloud
cd D:\MyPodSync\MyPodSync\MyPodSync.Sync\bin\Debug\net8.0
MyPodSync.Sync.exe sync --source "D:\MyPodSync\acishare\files"