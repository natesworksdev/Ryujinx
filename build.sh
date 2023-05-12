#!/bin/bash -e
./distribution/macos/create_macos_release.sh . ./build/temp ./build/output ./distribution/macos/entitlements.xml 1.1.0 $(date +%Y%m%d) Release
