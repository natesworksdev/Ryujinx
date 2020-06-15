
<h1>
    <img src="https://i.imgur.com/G6Mleco.png"> Ryujinx
    <a href="https://ci.appveyor.com/project/gdkchan/ryujinx?branch=master" target="_blank">
        <img src="https://ci.appveyor.com/api/projects/status/ssg4jwu6ve3k594s/branch/master?svg=true">
    </a>
    <a href="https://discord.gg/N2FmfVc">
        <img src="https://img.shields.io/discord/410208534861447168.svg">
    </a>
</h1>

<p align="center">
    <i>An experimental Switch emulator written in C#</i><br />
    <br />
    <img src="https://i.imgur.com/JDLmXJ6.png">
</p>

<h5 align="center">
    As of June 2020, Ryujinx goes past menus and in-game on over 1,000 commercial titles. Of those, roughly half are considered playable. See the compatiblity list <a href="https://github.com/Ryujinx/Ryujinx-Games-List/issues" target="_blank">here</a>.
</h5>

## Usage

To run this emulator, we recommend that your PC have at least 8GB of RAM; less than this amount can result in unpredictable behavior and may cause crashes or unacceptable performance.
If you use a pre-built version, you can use the graphical interface to run your games and homebrew: simply add the directory containing your homebrew or games in the Options > Settings > General tab > Game Directories menu item.

If you build it yourself you will need to:
Step one: Install the [.NET Core 3.1 (or higher) SDK](https://dotnet.microsoft.com/download/dotnet-core).
Step two (Variant one):

After the installation of the Net Core SDK is done; go ahead and copy the Clone link from GitHub from here (via Clone or Download --> Copy HTTPS Link. Or you can download the ZIP tarball.) You can Git Clone the repo by using the GitBash, or you may use the second variant.

Step two (Variant two):

Download the ZIP Tarball. Then extract it to a directory of your choice.

Step three:

Build the App using a Command prompt in the ROOT directory. You can quickly access it by Holding shift in explorer (in the Ryujinx directory) then right clicking, and typing the following command
Run `dotnet build -c Release -r win10-x64` inside the Ryujinx project folder to build Ryujinx binaries.

Every file related to Ryujinx is stored in the `Ryujinx` folder. Located in `C:\Users\USERNAME\AppData\Roaming\` for Windows, `/home/USERNAME/.config` for Linux or `/Users/USERNAME/Library/Application Support/` for macOS. It can also be accessed by clicking `Open Ryujinx Folder` under the File menu in the GUI.

## Latest build

These builds are compiled automatically for each commit on the master branch. While we strive to ensure optimal stability and performance prior to pushing an update, even though they are official these builds **may be unstable or completely broken.**

The latest automatic build for Windows, macOS, and Linux can be found on the [Official Website](https://ryujinx.org/download).

## Requirements

 - **Switch Keys**

   Everything on the Switch is encrypted, so if you want to run anything other than homebrew, you have to dump encryption keys from your console. To get more information please take a look at our [Keys Documentation](KEYS.md).

 - **Firmware**
    
    You need an official Switch firmware by either dumping directly from your Switch or dumping your cartridge game to an XCI file. Install the firmware, after you've installed your keys, from the Tools > Install Firmware menu item.

 - **Executables**

   Ryujinx is able to run both official games and homebrew.

   Homebrew is available on many websites, such as the [Switch Appstore](https://www.switchbru.com/appstore/).

   A hacked Nintendo Switch is needed to dump games, which you can learn how to do [here](https://nh-server.github.io/switch-guide/). Once you have hacked your Nintendo Switch, you will need to dump your own games with [NxDumpTool](https://github.com/DarkMatterCore/nxdumptool/releases) to get an XCI or NSP dump.

## Features

 - **Audio**

   Everything for audio is partially supported. We currently use a C# wrapper for [libsoundio](http://libsound.io/), and we support [OpenAL](https://openal.org/downloads/OpenAL11CoreSDK.zip) (installation needed) too as a fallback. Our current Opus implementation is incomplete and negatively affects many games.

- **CPU**

  The CPU emulator, ARMeilleure, emulates an ARMv8 CPU, and currently only has support for the new 64-bit ARMv8 instructions (with a few instructions still missing). It translates the ARM code to a custom IR, performs a few optimizations, and turns that into x86 code. To handle that, we use our own JIT called ARMeilleure, which uses the custom IR and compiles the code to x86.

- **GPU**

  The GPU emulator emulates the Switch's Maxwell GPU using the OpenGL API (version 4.4 minimum) through a custom build of OpenTK.

- **Input**

   We currently have support for keyboard, mouse, touch input, JoyCon input support emulated through the keyboard, and Xinput-compatible controllers too. You can set up everything inside the configuration menu.

- **Configuration**

   The emulator has settings for enabling or disabling some logging, remapping controllers, and more. You can configure all of them through the graphical interface or manually through the config file, `Config.json`.

   For more information [you can go here](CONFIG.md) *(Outdated)*.

## Compatibility

You can check out the compatibility list [here](https://github.com/Ryujinx/Ryujinx-Games-List/issues).

Don't hesitate to open a new issue if a game isn't already on there.

## Help

If you have homebrew or a particular game marked playable or in-game in our compatibility list that doesn't work within the emulator, you can contact us through our Discord. We'll take note of whatever is causing the app/game to not work, put it on the watch list and fix it at a later date.

If you need help with setting up Ryujinx, you can ask questions in the #support channel of our Discord server.

## Contact

If you have contributions, need support, have suggestions, or just want to get in touch with the team, join our [Discord server](https://discord.gg/N2FmfVc)!

If you'd like to donate, please take a look at our [Patreon](https://www.patreon.com/ryujinx).
