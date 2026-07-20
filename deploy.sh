#!/bin/bash
# Script de deploy para a Oracle Cloud (Ubuntu ARM)

echo "🚀 Iniciando deploy do Wyvern..."

# 1. Puxa as novidades do Github
echo "📦 Atualizando repositório..."
git pull origin master

# 2. Faz o build das imagens Docker e sobe em background
echo "🐳 Reconstruindo contêineres..."
docker compose build
docker compose down
docker compose up -d

echo "✅ Deploy concluído com sucesso!"
echo "Status dos contêineres:"
docker compose ps
