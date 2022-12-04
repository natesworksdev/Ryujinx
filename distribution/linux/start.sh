#!/usr/bin/env sh
SCRIPT_DIR=$(dirname $0)
chmod +x "$SCRIPT_DIR/Ryujinx"

DOTNET_EnableAlternateStackCheck=1 GDK_BACKEND=x11 "$SCRIPT_DIR/Ryujinx" "$@"

exit
