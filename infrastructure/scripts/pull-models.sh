#!/bin/bash
# pull-models.sh — Pull required Ollama models into the Docker container
# Usage: docker exec rag-ollama bash /scripts/pull-models.sh
#   OR:  docker compose exec ollama bash -c "ollama pull llama3.2 && ollama pull mxbai-embed-large"

set -e

echo "=== Pulling Ollama models ==="

echo "Pulling mxbai-embed-large (embedding model)..."
ollama pull mxbai-embed-large

echo "Pulling llama3.2 (generation model)..."
ollama pull llama3.2

echo ""
echo "=== Models ready ==="
ollama list
