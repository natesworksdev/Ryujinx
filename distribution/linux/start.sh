#!/usr/bin/env sh
# It is also recommended to run start.sh in the terminal, although this isn't required in order for it to operate
SCRIPT_DIR=$(dirname $0)
chmod +x "$SCRIPT_DIR/Ryujinx"

DOTNET_EnableAlternateStackCheck=1 GDK_BACKEND=x11 "$SCRIPT_DIR/Ryujinx" "$@"

exit
