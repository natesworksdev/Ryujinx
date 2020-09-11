#!/bin/bash -ex

mkdir -p AppDir/usr/bin
cp Ryujinx/bin/Release/netcoreapp3.1/linux-x64/publish/* AppDir/usr/bin/
cp Ryujinx/Ui/assets/Icon.png AppDir/Ryujinx.png
cp Ryujinx/Ui/assets/Ryujinx.desktop AppDir/Ryujinx.desktop
curl -sL https://github.com/AppImage/AppImageKit/releases/download/continuous/AppRun-x86_64 -o AppDir/AppRun
curl -sL https://github.com/AppImage/AppImageKit/releases/download/continuous/runtime-x86_64 -o ./AppDir/runtime
mkdir -p AppDir/usr/share/applications && cp ./AppDir/Ryujinx.desktop ./AppDir/usr/share/applications
mkdir -p AppDir/usr/share/icons && cp ./AppDir/Ryujinx.png ./AppDir/usr/share/icons
mkdir -p AppDir/usr/share/icons/hicolor/scalable/apps && cp ./AppDir/Ryujinx.png ./AppDir/usr/share/icons/hicolor/scalable/apps
mkdir -p AppDir/usr/share/pixmaps && cp ./AppDir/Ryujinx.png ./AppDir/usr/share/pixmaps

chmod a+x ./AppDir/AppRun
chmod a+x ./AppDir/runtime
chmod a+x ./AppDir/usr/bin/Ryujinx

curl -sLO "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
chmod a+x appimagetool-x86_64.AppImage

./appimagetool-x86_64.AppImage AppDir/
mv Ryujinx-x86_64.AppImage ryujinx$config_name$APPVEYOR_BUILD_VERSION-linux_x64.AppImage
