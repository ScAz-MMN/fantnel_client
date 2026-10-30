#!/bin/bash
# Frontend dev server (Linux)  --  replaces B.bat
# Runs `npm run dev` (Vite) inside the web/ folder.
cd "$(dirname "$0")"

echo "============================================"
echo "  Fantnel Frontend (Vite dev server)"
echo "  Dir: $PWD"
echo "============================================"

if [ ! -d "node_modules" ]; then
    echo "[INFO] node_modules not found, running npm install ..."
    npm install
fi

echo "[START] npm run dev"
npm run dev