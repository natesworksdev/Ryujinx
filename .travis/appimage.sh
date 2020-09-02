#!/bin/bash -ex

mkdir -p AppDir/usr/bin
cp Ryujinx/bin/Release/netcoreapp3.1/linux-x64/publish/* AppDir/usr/bin/
cp Ryujinx/Ui/assets/Icon.png AppDir/Ryujinx.png
cp Ryujinx.desktop AppDir/Ryujinx.desktop
cp .travis/update.sh AppDir/update.sh
curl -sL https://github.com/RPCS3/AppImageKit-checkrt/releases/download/continuous2/AppRun-patched-x86_64 -o AppDir/AppRun
curl -sL https://github.com/AppImage/AppImageKit/releases/download/continuous/runtime-x86_64 -o ./AppDir/runtime
mkdir -p AppDir/usr/share/applications && cp ./AppDir/Ryujinx.desktop ./AppDir/usr/share/applications
mkdir -p AppDir/usr/share/icons && cp ./AppDir/Ryujinx.png ./AppDir/usr/share/icons
mkdir -p AppDir/usr/share/icons/hicolor/scalable/apps && cp ./AppDir/Ryujinx.png ./AppDir/usr/share/icons/hicolor/scalable/apps
mkdir -p AppDir/usr/share/pixmaps && cp ./AppDir/Ryujinx.png ./AppDir/usr/share/pixmaps
mkdir -p AppDir/usr/optional/ ; mkdir -p squashfs-root/usr/optional/libstdc++/
mkdir -p AppDir/usr/share/zenity 
cp /usr/share/zenity/zenity.ui ./AppDir/usr/share/zenity/
cp /usr/bin/zenity ./AppDir/usr/bin/
cp .travis/Config.json ./AppDir/usr/share/
#curl -sL https://github.com/RPCS3/AppImageKit-checkrt/releases/download/continuous2/exec-x86_64.so -o ./AppDir/usr/optional/exec.so
#cp /usr/lib/x86_64-linux-gnu/libstdc++.so.6 AppDir/usr/optional/libstdc++/
#printf "#include <bits/stdc++.h>\nint main(){std::make_exception_ptr(0);std::pmr::get_default_resource();}" | $CXX -x c++ -std=c++2a -o $HOME/squashfs-root/usr/optional/checker -

chmod a+x ./AppDir/AppRun
chmod a+x ./AppDir/runtime
chmod a+x ./AppDir/usr/bin/Ryujinx
chmod a+x ./AppDir/update.sh

cp .travis/update.tar.gz .
tar -xzf update.tar.gz
mv update/AppImageUpdate ./AppDir/usr/bin/
mkdir -p AppDir/usr/lib/
mv update/* ./AppDir/usr/lib/

echo $TRAVIS_COMMIT > ./AppDir/version.txt


ls -al ./AppDir

wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
chmod a+x appimagetool-x86_64.AppImage
./appimagetool-x86_64.AppImage AppDir/ -u "gh-releases-zsync|qurious-pixel|Ryujinx|continuous|Ryujinx-x86_64.AppImage.zsync"

curl --upload-file Ryujinx-x86_64.AppImage https://transfersh.com/Ryujinx-x86_64.AppImage
