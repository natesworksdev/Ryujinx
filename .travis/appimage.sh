#!/bin/bash -ex

mkdir -p AppDir/usr/bin
cp Ryujinx/bin/Release/netcoreapp3.1/linux-x64/publish/* AppDir/usr/bin/
cp Ryujinx/Ui/assets/Icon.png AppDir/Ryujinx.png
curl -sL https://github.com/RPCS3/AppImageKit-checkrt/releases/download/continuous2/AppRun-patched-x86_64 -o AppRun/AppRun
curl -sL https://github.com/AppImage/AppImageKit/releases/download/continuous/runtime-x86_64 -o ./AppDir/runtime
mkdir -p AppDir/usr/share/applications && cp ./AppDir/Ryujinx.desktop ./AppDir/usr/share/applications
mkdir -p AppDir/usr/share/icons && cp ./AppDir/Ryujinx.png ./AppDir/usr/share/icons
mkdir -p AppDir/usr/share/icons/hicolor/scalable/apps && cp ./AppDir/Ryujinx.png ./AppDir/usr/share/icons/hicolor/scalable/apps
mkdir -p AppDir/usr/share/pixmaps && cp ./AppDir/Ryujinx.png ./AppDir/usr/share/pixmaps

chmod a+x ./AppDir/AppRun
chmod a+x ./AppDir/runtime
chmod a+x ./AppDir/usr/bin/Ryujinx

wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
chmod a+x appimagetool-x86_64.AppImage
./appimagetool-x86_64.AppImage AppDir/
