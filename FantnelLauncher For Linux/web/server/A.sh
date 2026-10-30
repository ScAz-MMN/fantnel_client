#!/bin/bash
# MySQL bridge server (Linux)  --  replaces A.bat
# Runs `npm start` (node server.js) inside the web/server/ folder.
cd "$(dirname "$0")"

echo "============================================"
echo "  Fantnel MySQL Bridge (port 3939)"
echo "  Dir: $PWD"
echo "============================================"

if [ ! -d "node_modules" ]; then
    echo "[INFO] node_modules not found, running npm install ..."
    npm install
fi

echo "[START] npm start"
npm start