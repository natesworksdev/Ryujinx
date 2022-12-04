#!/usr/bin/env sh
# We recommend using Pine-jinx if you want the game to be optimised and installed for your system. This is mostly for PR builds and people who want a portable install.
# It is also recommended to run start.sh in the terminal, although this isn't required in order for it to operate
SCRIPT_DIR=$(dirname $0)
chmod +x "$SCRIPT_DIR/Ryujinx"

DOTNET_EnableAlternateStackCheck=1 GDK_BACKEND=x11 "$SCRIPT_DIR/Ryujinx" "$@"

exit
