#!/bin/bash

set -e

INSTALL_DIRECTORY=$1
NEW_APP_DIRECTORY=$2
APP_PID=$3
APP_ARGUMENTS="${@:4}"

error_handler() {
    local lineno="$1"

    script="""
    set alertTitle to \"Ryujinx - Updater error\"
    set alertMessage to \"An error occurred during Ryujinx update (updater.sh:$lineno)\n\nPlease download the update manually from our website if the problem persists.\"
    display dialog alertMessage with icon caution with title alertTitle buttons {\"Open Download Page\", \"Exit\"}
    set the button_pressed to the button returned of the result

    if the button_pressed is \"Open Download Page\" then
        open location \"https://ryujinx.org/download\"
    end if
    """

    osascript -e "$script"
    exit 1
}

trap 'error_handler ${LINENO}' ERR

# Wait for Ryujinx to exit
# If the main process as well as child processes are still acitve, we wait for 1 second and check it again.
# After the third time checking, this script exits with status 1

attempt=0

while true; do
    process_status=$(ps -p "$APP_PID" -o state=)
    child_processes=$(pgrep -P "$APP_PID")
    if [ -n "$process_status" ] || [ -n "$child_processes" ]; then
        if [ "$attempt" -eq 2 ]; then
            exit 1
        fi
        sleep 1
    else
        break
    fi
    (( attempt++ ))
done

# Now replace and reopen.
rm -rf "$INSTALL_DIRECTORY"
mv "$NEW_APP_DIRECTORY" "$INSTALL_DIRECTORY"

if [ "$#" -le 3 ]; then
    open -a "$INSTALL_DIRECTORY"
else
    open -a "$INSTALL_DIRECTORY" --args "$APP_ARGUMENTS"
fi
