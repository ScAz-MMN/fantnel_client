#!/bin/bash
# ============================================================
#  Fantnel one-click launcher (Linux)
#  Opens SEPARATE terminal windows, one per component:
#    1. Backend      : back/fantnel/fantnel.sh   (dotnet Fantnel.dll)
#    2. Frontend     : web/B.sh                  (npm run dev)
#    3. MySQL bridge : web/server/A.sh           (npm start)
#  Each component runs in its own shell window so you can watch
#  its logs independently and close that component by closing
#  its window.
#  Run:  bash st.sh   (or chmod +x st.sh && ./st.sh)
# ============================================================

cd "$(dirname "$0")"
echo "============================================"
echo "  Fantnel Launcher (Linux)"
echo "  Work dir: $PWD"
echo "============================================"
echo

# ---------- detect a desktop terminal emulator ----------
TERM_BIN=""
TERM_OPEN=""   # command that opens one terminal window with:  <title> <command...>
if   command -v gnome-terminal  >/dev/null 2>&1; then TERM_BIN=gnome-terminal;  TERM_OPEN=gnome_terminal
elif command -v xfce4-terminal  >/dev/null 2>&1; then TERM_BIN=xfce4-terminal;  TERM_OPEN=xfce_terminal
elif command -v mate-terminal  >/dev/null 2>&1;  then TERM_BIN=mate-terminal;   TERM_OPEN=mate_terminal
elif command -v konsole  >/dev/null 2>&1;        then TERM_BIN=konsole;         TERM_OPEN=konsole
elif command -v xterm  >/dev/null 2>&1;          then TERM_BIN=xterm;           TERM_OPEN=xterm
elif command -v kitty  >/dev/null 2>&1;          then TERM_BIN=kitty;           TERM_OPEN=kitty
fi

# Each terminal emulator has a different way of (a) setting a title and
# (b) running a command in a new window. We pick per emulator.
gnome_terminal() { # $1=title  $2=command-string
    gnome-terminal --title="$1" -- bash -lc "$2"
}
xfce_terminal() { # $1=title  $2=command-string
    xfce4-terminal --title="$1" -e "bash -lc \"$2\""
}
mate_terminal() { # $1=title  $2=command-string
    mate-terminal --title="$1" -e "bash -lc \"$2\""
}
konsole() { # $1=title  $2=command-string
    konsole --title "$1" -e bash -lc "$2"
}
xterm() { # $1=title  $2=command-string
    xterm -T "$1" -e bash -lc "$2"
}
kitty() { # $1=title  $2=command-string
    kitty --title "$1" bash -lc "$2"
}

# Start one component in its own terminal window (or background fallback).
#   $1 = component label   $2 = working dir   $3 = command string
launch_one() {
    local label="$1" dir="$2" cmd="$3"
    if [ -z "$TERM_BIN" ]; then
        echo "[$label] No desktop terminal found; running in background."
        ( cd "$dir" && bash -lc "$cmd" ) &
        return
    fi
    echo "[$label] Opening terminal window..."
    $TERM_OPEN "$label" "cd '$dir' && $cmd"
}

# ---------- 1. Start backend ----------
echo "[1/3] Starting backend"
if [ -d "back/fantnel" ] && [ -f "back/fantnel/Fantnel.dll" ]; then
    launch_one "Fantnel-Backend" "$PWD/back/fantnel" "dotnet Fantnel.dll"
else
    echo "      [WARN] back/fantnel/Fantnel.dll NOT found, skipped."
fi
echo

# ---------- 2. Start frontend ----------
echo "[2/3] Starting frontend"
if [ -f "web/B.sh" ]; then
    launch_one "Fantnel-Web" "$PWD/web" "bash B.sh"
else
    echo "      [WARN] web/B.sh NOT found, skipped."
fi
echo

# ---------- 3. Start MySQL bridge ----------
echo "[3/3] Starting MySQL bridge"
if [ -f "web/server/A.sh" ]; then
    launch_one "Fantnel-MySQL-Bridge" "$PWD/web/server" "bash A.sh"
else
    echo "      [WARN] web/server/A.sh NOT found, skipped."
fi
echo

echo "============================================"
echo "  3 terminal windows opened (one per component)."
echo "  Frontend dev server: http://localhost:5173"
echo "  Close a component's window to stop it."
echo "============================================"

exit 0