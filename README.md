
<h1 align="center">
  <br>
  <a href="https://ryujinx.org/"><img src="https://i.imgur.com/WcCj6Rt.png" alt="Ryujinx" width="150"></a>
  <br>
  <b>Ryujinx</b>
  <br>
  <sub><sup><b>(REE-YOU-JINX)</b></sup></sub>
  <br>

</h1>

<p align="center">
       Ryujinx is an open-source Nintendo Switch emulator, created by gdkchan, written in C#.
       This emulator aims at providing excellent accuracy and performance, a user-friendly interface and consistent builds.
    It was written from scratch and development on the project began in September 2017. Ryujinx is available on Github under the <a href="https://github.com/Ryujinx/Ryujinx/blob/master/LICENSE.txt" target="_blank">MIT license</a>. <br /> 

</p>
<p align="center">
    <a href="https://github.com/Ryujinx/Ryujinx/actions/workflows/release.yml">
        <img src="https://github.com/Ryujinx/Ryujinx/actions/workflows/release.yml/badge.svg"
            alt="">
    </a>
    <a href="https://discord.com/invite/VkQYXAZ">
        <img src="https://img.shields.io/discord/410208534861447168?color=5865F2&label=Ryujinx&logo=discord&logoColor=white"
            alt="Discord">
    </a>
    <br>
    <br>
    <img src="https://raw.githubusercontent.com/Ryujinx/Ryujinx-Website/master/static/public/shell_fullsize.png">
</p>

<h5 align="center">
  
</h5>

## Compatibility

As of January 2022, Ryujinx has been tested on approximately 3,500 titles; over 3,200 boot past menus and into gameplay, with roughly 2,500 of those being considered playable.
You can check out the compatibility list [here](https://github.com/Ryujinx/Ryujinx-Games-List/issues). Anyone is free to submit an updated test on an existing game entry; simply follow the new issue template and testing guidelines, or post as a reply to the applicable game issue. Use the search function to see if a game has been tested already!
                  
## Usage

To run this emulator, your PC must be equipped with at least 8GB of RAM; failing to meet this requirement may result in a poor gameplay experience or unexpected crashes.

See our [Setup & Configuration Guide](https://github.com/Ryujinx/Ryujinx/wiki/Ryujinx-Setup-&-Configuration-Guide) on how to set up the emulator.

For our Local Wireless and LAN builds, see our [Multiplayer: Local Play/Local Wireless Guide
](https://github.com/Ryujinx/Ryujinx/wiki/Multiplayer-(LDN-Local-Wireless)-Guide).

## Latest build

These builds are compiled automatically for each commit on the master branch. While we strive to ensure optimal stability and performance prior to pushing an update, our automated builds **may be unstable or completely broken.**

If you want to see details on updates to the emulator, you can visit our [Changelog](https://github.com/Ryujinx/Ryujinx/wiki/Changelog).

The latest automatic build for Windows, macOS, and Linux can be found on the [Official Website](https://ryujinx.org/download).


## Building

Follow these steps to build the emulator on your computer. Please note that we do not offer support for custom builds.

### Step 1
Install the x64 version of [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) for your operating system.

### Step 2
Either clone the repo by using `git clone https://github.com/Ryujinx/Ryujinx`, or download the repo directly from the web interface by clicking the green <strong>Code</strong> button > <strong>Download ZIP</strong>.

### Step 3
To build Ryujinx, open the command prompt or terminal and use the `cd` command to navigate to the Ryujinx project folder. Windows users can also quickly access it by holding <kbd>Shift</kbd> in File Explorer, then right clicking and selecting `Open command window here`.

After that, type the following command:  
`dotnet publish -c Release -r win-x64 -o build-ryujinx-custom /p:DebugType=embedded Ryujinx --self-contained`

Change `win-x64` to `linux-x64` if you use Linux.

The output build files will be in the `build-ryujinx-custom` folder. Excluding that folder, you can delete the project files to save disk space.

## Features

 - **Audio**

   Audio output is entirely supported, audio input (microphone) isn't supported. We use C# wrappers for [OpenAL](https://openal-soft.org/), and [SDL2](https://www.libsdl.org/) & [libsoundio](http://libsound.io/) as fallbacks.

- **CPU**

  The CPU emulator, ARMeilleure, emulates an ARMv8 CPU and currently has support for most 64-bit ARMv8 and some of the ARMv7 (and older) instructions, including partial 32-bit support. It translates the ARM code to a custom IR, performs a few optimizations, and turns that into x86 code.  
  There are three memory manager options available depending on the user's preference, leveraging both software-based (slower) and host-mapped modes (much faster). The fastest option (host, unchecked) is set by default.
  Ryujinx also features an optional Profiled Persistent Translation Cache, which essentially caches translated functions so that they do not need to be translated every time the game loads. The net result is a significant reduction in load times (the amount of time between launching a game and arriving at the title screen) for nearly every game. NOTE: this feature is enabled by default in the Options menu > System tab. You must launch the game at least twice to the title screen or beyond before performance improvements are unlocked on the third launch! These improvements are permanent and do not require any extra launches going forward.

- **GPU**

  The GPU emulator emulates the Switch's Maxwell GPU using the OpenGL API (version 4.5 minimum) through a custom build of OpenTK. There are currently four graphics enhancements available to the end user in Ryujinx: disk shader caching, resolution scaling, aspect ratio adjustment and anisotropic filtering. These enhancements can be adjusted or toggled as desired in the GUI.

- **Input**

   We currently have support for keyboard, mouse, touch input, JoyCon input support, and nearly all controllers. Motion controls are natively supported in most cases; for dual-JoyCon motion support, DS4Windows or BetterJoy are currently required. 
   In all scenarios, you can set up everything inside the input configuration menu.

- **DLC & Modifications**

   Ryujinx is able to manage add-on content/downloadable content through the GUI. Mods (romfs, exefs, and runtime mods such as cheats) are also supported; the GUI contains a shortcut to open the respective mods folder for a particular game.

- **Configuration**

   The emulator has settings for enabling or disabling some logging, remapping controllers, and more. You can configure all of them through the graphical interface or manually through the config file, `Config.json`, found in the user folder which can be accessed by clicking `Open Ryujinx Folder` under the File menu in the GUI.


## Contact

If you have contributions, suggestions, need emulator support or just want to get in touch with the team, join our [Discord server](https://discord.com/invite/Ryujinx). You may also review our [FAQ](https://github.com/Ryujinx/Ryujinx/wiki/Frequently-Asked-Questions).

## Donations
                  
If you'd like to support the project financially, Ryujinx has an active Patreon campaign.

<a href="https://www.patreon.com/ryujinx">
    <img src="https://images.squarespace-cdn.com/content/v1/560c1d39e4b0b4fae0c9cf2a/1567548955044-WVD994WZP76EWF15T0L3/Patreon+Button.png?format=500w" width="150">
</a>

All the developers working on the project do so on their free time, but the project has several expenses:
* Hackable Nintendo Switch consoles to reverse-engineer the hardware
* Additional computer hardware for testing purposes (e.g. GPUs to diagnose graphical bugs, etc.) 
* Licenses for various software development tools (e.g. Jetbrains, LDN servers, IDA)
* Web hosting and infrastructure maintenance
 
All funds received through Patreon are considered a donation to support the project. Patrons receive early access to progress reports and exclusive access to developer interviews.

## License

This software is licensed under the terms of the <a href="https://github.com/Ryujinx/Ryujinx/blob/master/LICENSE.txt" target="_blank">MIT license.</a></i><br /> 
The Ryujinx.Audio project is licensed under the terms of the <a href="https://github.com/Ryujinx/Ryujinx/blob/master/Ryujinx.Audio/LICENSE.txt
" target="_blank">LGPLv3 license.</a></i><br />
This project makes use of code authored by the libvpx project, licensed under BSD and the ffmpeg project, licensed under LGPLv3.
See [LICENSE.txt](LICENSE.txt) and [THIRDPARTY.md](Ryujinx/THIRDPARTY.md) for more details.
## Credits

- [LibHac](https://github.com/Thealexbarney/LibHac) is used for our file-system. 
- [AmiiboAPI](https://www.amiiboapi.com) is used in our Amiibo emulation.
