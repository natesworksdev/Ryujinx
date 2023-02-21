#!/bin/bash

set -e

INSTALL_DIRECTORY=$1
NEW_APP_DIRECTORY=$2
APP_ARGUMENTS="${@:3}"

# Wait 1s to give Ryujinx some time to exit.
sleep 1

# Now replace and reopen.
rm -rf $INSTALL_DIRECTORY
mv $NEW_APP_DIRECTORY/Ryujinx.app $INSTALL_DIRECTORY
open -a $INSTALL_DIRECTORY --args $APP_ARGUMENTS
