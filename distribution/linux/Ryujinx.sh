#!/bin/sh

SCRIPT_DIR=$(dirname "$(realpath "$0")")
RYUJINX_BIN="Ryujinx"

if [ -f "$SCRIPT_DIR/Ryujinx.Ava" ]; then
    RYUJINX_BIN="Ryujinx.Ava"
fi

if [ -f "$SCRIPT_DIR/Ryujinx.Headless.SDL2" ]; then
    RYUJINX_BIN="Ryujinx.Headless.SDL2"
fi

gamemode="$(which gamemoderun)"

case "which: no gamemoderun in" in
*"$gamemode"*) env DOTNET_EnableAlternateStackCheck=1 "$SCRIPT_DIR/$RYUJINX_BIN" "$@";;
*   ) env DOTNET_EnableAlternateStackCheck=1 "$gamemode" "$SCRIPT_DIR/$RYUJINX_BIN" "$@";;
esac
