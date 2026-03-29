#!/bin/bash
# deploy.sh — One-command Azure deployment
# Usage: ./deploy.sh <resource-group> <sql-admin-password>
#
# Prerequisites:
#   - Azure CLI logged in (az login)
#   - Resource group already created

set -e

RESOURCE_GROUP="${1:?Usage: ./deploy.sh <resource-group> <sql-admin-password>}"
SQL_PASSWORD="${2:?Provide SQL admin password as second argument}"
LOCATION="eastus"

echo "=== Deploying to Azure ==="
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo ""

# Create resource group if it doesn't exist
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none 2>/dev/null || true

# Deploy Bicep
echo "Deploying infrastructure..."
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file main.bicep \
  --parameters parameters.json \
  --parameters sqlAdminPassword="$SQL_PASSWORD" \
  --output table

echo ""
echo "=== Deployment complete ==="
az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name main \
  --query "properties.outputs" \
  --output table
