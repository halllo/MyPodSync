# MyPodSync

Utilizing [Podsync](https://github.com/mxpv/podsync).

## Configuration

```toml
[server]
port = 80
hostname = "https://..."
data_dir = "/app/data/files"

[downloader]
self_update = true # Optional, auto update youtube-dl every 24 hours
timeout = 40

[tokens]
youtube = "..."
vimeo = "..."

[feeds]
  [feeds.ManuelsChannel]
  url = "https://www.youtube.com/channel/UC6HL02uT6PED5SGDeX9Tk_g"
  page_size = 10
  update_period = "24h"
  quality = "low"
  format = "audio"
  clean = { keep_last = 10 }
```

## Runtime

### Azure Container Instance

```powershell
# Get-ExecutionPolicy
# Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

$ACI_PERS_RESOURCE_GROUP = "PodSync"
$ACI_PERS_STORAGE_ACCOUNT_NAME = "psdata$(Get-Random)"
$ACI_PERS_LOCATION = "westeurope"
$ACI_PERS_SHARE_NAME = "acishare"
$ACI_CONTAINER_NAME = "pscontainer$(Get-Random)"
$ACI_CONTAINER_DNS = "..."

# Create the storage account
az storage account create `
    --resource-group $ACI_PERS_RESOURCE_GROUP `
    --name $ACI_PERS_STORAGE_ACCOUNT_NAME `
    --location $ACI_PERS_LOCATION `
    --sku Standard_LRS

# Create the file share
az storage share create --name $ACI_PERS_SHARE_NAME --account-name $ACI_PERS_STORAGE_ACCOUNT_NAME

$STORAGE_KEY = $(az storage account keys list --resource-group $ACI_PERS_RESOURCE_GROUP --account-name $ACI_PERS_STORAGE_ACCOUNT_NAME --query "[0].value" --output tsv)

# Create the PodSync container
az container create `
    --resource-group $ACI_PERS_RESOURCE_GROUP `
    --name $ACI_CONTAINER_NAME `
    --image ghcr.io/mxpv/podsync:latest `
    --dns-name-label $ACI_CONTAINER_DNS `
    --ports 8080 80 443 `
    --azure-file-volume-account-name $ACI_PERS_STORAGE_ACCOUNT_NAME `
    --azure-file-volume-account-key $STORAGE_KEY `
    --azure-file-volume-share-name $ACI_PERS_SHARE_NAME `
    --azure-file-volume-mount-path /app/data `
    --environment-variables PODSYNC_CONFIG_PATH="/app/data/config.toml"
```

#### Update Container Image

```powershell
$ACI_PERS_RESOURCE_GROUP = "PodSync"
$ACI_PERS_STORAGE_ACCOUNT_NAME = "psdata..."
$ACI_PERS_LOCATION = "westeurope"
$ACI_PERS_SHARE_NAME = "acishare"
$ACI_CONTAINER_NAME = "pscontainer..."
$ACI_CONTAINER_DNS = "..."

$STORAGE_KEY = $(az storage account keys list --resource-group $ACI_PERS_RESOURCE_GROUP --account-name $ACI_PERS_STORAGE_ACCOUNT_NAME --query "[0].value" --output tsv)

# Create the PodSync container
az container create `
    --resource-group $ACI_PERS_RESOURCE_GROUP `
    --name $ACI_CONTAINER_NAME `
    --image ghcr.io/mxpv/podsync:latest `
    --dns-name-label $ACI_CONTAINER_DNS `
    --ports 8080 80 443 `
    --azure-file-volume-account-name $ACI_PERS_STORAGE_ACCOUNT_NAME `
    --azure-file-volume-account-key $STORAGE_KEY `
    --azure-file-volume-share-name $ACI_PERS_SHARE_NAME `
    --azure-file-volume-mount-path /app/data `
    --environment-variables PODSYNC_CONFIG_PATH="/app/data/config.toml"
```

Unfortunately ACI seems to not be allowed YT download anymore:

> time="2024-10-30T14:59:35Z" level=error msg="[youtube] Extracting URL: <https://youtube.com/watch?v=\n[youtube>] ...: Downloading webpage\n[youtube] Sleeping 1.5 seconds ...\n[youtube] ...: Downloading ios player API JSON\n[youtube] Sleeping 1.5 seconds ...\n[youtube] ...: Downloading mweb player API JSON\nERROR: [youtube] ...: Sign in to confirm you're not a bot. This helps protect our community. Learn more\n"

According to <https://github.com/mxpv/podsync/issues/645#issuecomment-2375537795> it helps to add a PO Token (Proof of Origin) and a cookies.txt

- [Getting PO Token](https://github.com/yt-dlp/yt-dlp/wiki/Extractors#po-token-guide)
- [Getting youtube cookies into cookies.txt](https://chromewebstore.google.com/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc)

Unfortunately it still didn't work for my Azure Container Instance so I fell back to running it locally.

### Local

```powershell
podman run -p 5055:8080 --rm -it -v D:\MyPodSync\acishare:/app/data/ -e "PODSYNC_CONFIG_PATH=/app/data/config.toml" ghcr.io/mxpv/podsync:latest
```
