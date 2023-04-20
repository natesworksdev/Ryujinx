#!/bin/sh

SCRIPT_DIR=$(dirname "$(realpath "$0")")
RYUJINX_BIN="Ryujinx"

if [ -f "$SCRIPT_DIR/Ryujinx.Ava" ]; then
    RYUJINX_BIN="Ryujinx.Ava"
fi

if [ -f "$SCRIPT_DIR/Ryujinx.Headless.SDL2" ]; then
    RYUJINX_BIN="Ryujinx.Headless.SDL2"
fi

ORIG_MAX_MAP_COUNT=$(sysctl vm.max_map_count | awk -F' ' '{print $3}')

if [ $ORIG_MAX_MAP_COUNT -le 65530 ]; then
  echo "Increasing max amount of memory map areas..."
  pkexec sh -c 'sysctl -w vm.max_map_count=262144'
fi

COMMAND="env DOTNET_EnableAlternateStackCheck=1"

if command -v gamemoderun > /dev/null 2>&1; then
    COMMAND="$COMMAND gamemoderun"
fi

$COMMAND "$SCRIPT_DIR/$RYUJINX_BIN" "$@"

if [ $ORIG_MAX_MAP_COUNT -le 65530 ]; then
  echo "Restoring original max amount of memory map areas..."
  pkexec sh -c "sysctl -w vm.max_map_count=$ORIG_MAX_MAP_COUNT"
fi