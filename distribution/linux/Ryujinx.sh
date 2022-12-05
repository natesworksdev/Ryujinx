#!/usr/bin/env sh
SCRIPT_DIR=$(dirname $(realpath $0))

chmod +x "$SCRIPT_DIR/Ryujinx"

DOTNET_EnableAlternateStackCheck=1 "$SCRIPT_DIR/Ryujinx" "$@"

exit
