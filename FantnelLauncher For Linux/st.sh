#!/bin/bash
# ============================================================
#  Fantnel one-click launcher (Linux)
#  Starts three components:
#    1. Backend      : back/fantnel  (dotnet Fantnel.dll)
#    2. Frontend     : web/B.sh       (npm run dev)
#    3. MySQL bridge : web/server/A.sh (npm start)
#  Run:  bash st.sh   (or chmod +x st.sh && ./st.sh)
# ============================================================

cd "$(dirname "$0")"
echo "============================================"
echo "  Fantnel Launcher (Linux)"
echo "  Work dir: $PWD"
echo "============================================"
echo

# ---------- 1. Start backend ----------
echo "[1/3] Starting backend: back/fantnel/Fantnel.dll"
if [ -f "back/fantnel/Fantnel.dll" ]; then
    (
        cd "back/fantnel"
        dotnet Fantnel.dll
    ) &
    echo "      [OK] Backend launched. Log goes to this terminal's background job."
else
    echo "      [WARN] back/fantnel/Fantnel.dll NOT found, skipped."
fi
echo

# ---------- 2. Start frontend ----------
echo "[2/3] Starting frontend: web/B.sh (npm run dev)"
if [ -f "web/B.sh" ]; then
    (
        cd "web"
        bash B.sh
    ) &
    echo "      [OK] Frontend launched."
else
    echo "      [WARN] web/B.sh NOT found, skipped."
fi
echo

# ---------- 3. Start MySQL bridge ----------
echo "[3/3] Starting MySQL bridge: web/server/A.sh (npm start)"
if [ -f "web/server/A.sh" ]; then
    (
        cd "web/server"
        bash A.sh
    ) &
    echo "      [OK] MySQL bridge launched."
else
    echo "      [WARN] web/server/A.sh NOT found, skipped."
fi
echo

echo "============================================"
echo "  All components launched."
echo "  Frontend dev server: http://localhost:5173"
echo "  Press Ctrl+C here to stop the 3 jobs."
echo "============================================"

# Keep the script alive so the background jobs stay attached
wait