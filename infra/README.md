# Azure Container Apps deployment

`main.bicep` deploys the SecureFix API to Azure Container Apps. It creates:

- a Log Analytics workspace and managed environment
- a user-assigned managed identity with `AcrPull` on an existing Azure Container Registry
- one externally reachable Container App with `/health` and `/ready` probes
- an encrypted Container Apps secret for the demo authentication token

Build and push the image to an Azure Container Registry first. The template expects the
image name to be `<registry>.azurecr.io/securefix:<image-tag>`.

```bash
az deployment group create \
  --resource-group <resource-group> \
  --template-file infra/main.bicep \
  --parameters containerRegistryName=<existing-acr-name> imageTag=<image-tag> \
  --parameters demoToken="$SECUREFIX_DEMO_TOKEN"
```

Do not supply a real token in `main.parameters.json` or commit it to source control.

## Demo limitation

This template uses SQLite at `/tmp/securefix.db`, which is appropriate only for the
single-replica hackathon demo and is not durable across revisions or replica changes.
Before production use, replace it with the documented PostgreSQL-backed persistence
implementation and configure the application with its managed-identity connection details.
