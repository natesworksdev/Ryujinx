
<h1 align="center">
  <br>
  <a href="https://ryujinx.org/"><img src="https://i.imgur.com/WcCj6Rt.png" alt="Ryujinx" width="150"></a>
  <br>
  <b>Ryujinx</b>
  <br>
  <sub><sup><b>(REE-YOU-JI-NX)</b></sup></sub>
  <br>

</h1>

<p align="center">
       Ryujinx is an open-source Nintendo Switch emulator created by gdkchan and is written in C#.
       This emulator aims at providing excellent accuracy and performance, while also providing a user-friendly interface, and consistent builds.
       It was written from scratch and development started around September of 2017. Ryujinx is available on GitHub under the <a href="https://github.com/Ryujinx/Ryujinx/blob/master/LICENSE.txt" target="_blank">MIT license</a></i><br /> 

</p>
<p align="center">
    <a href="https://ci.appveyor.com/project/gdkchan/ryujinx?branch=master">
        <img src="https://ci.appveyor.com/api/projects/status/ssg4jwu6ve3k594s/branch/master?svg=true"
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

As of September 2021, Ryujinx has been tested on nearly 3,400 titles: ~3,000 boot past menus and into gameplay, with approximately 2,400 of those being considered playable.
You can check out the compatibility list [here](https://github.com/Ryujinx/Ryujinx-Games-List/issues). Anyone is free to submit an updated test on an existing game entry; simply follow the new issue template and testing guidelines, and post as a reply to the applicable game issue.

Don't hesitate to open a new issue if a game isn't already on there!
                  
## Usage

To run this emulator, we recommend that your PC has at least 8GB of RAM; less than this amount can result in unpredictable behavior, crashes or unacceptable performance.

See our [Setup & Configuration Guide](https://github.com/Ryujinx/Ryujinx/wiki/Ryujinx-Setup-&-Configuration-Guide) on how to set up the emulator.

For our Local Wireless and LAN builds See our [Multiplayer: Local Play/Local Wireless Guide
](https://github.com/Ryujinx/Ryujinx/wiki/Multiplayer-(LDN-Local-Wireless)-Guide)

## Latest build

These builds are compiled automatically for each commit on the master branch. While we strive to ensure optimal stability and performance prior to pushing an update, our automated builds **may be unstable or completely broken.**

If you want to see what's changed in an update you can visit our [Changelog](https://github.com/Ryujinx/Ryujinx/wiki/Changelog).

The latest automatic build for Windows, macOS, and Linux can be found on the [Official Website](https://ryujinx.org/download).


## Building

If you wish to build the emulator yourself, follow these steps:

### Step 1
Install the X64 version of [.NET 6.0 (or higher) SDK](https://dotnet.microsoft.com/download/dotnet/6.0).

### Step 2
Either use `git clone https://github.com/Ryujinx/Ryujinx` to clone the repository or use the `Code>Download zip` button to get the files

### Step 3

To build Ryujinx open the Command prompt in the project directory. You can quickly access it by holding shift in the file explorer(while inside the Ryujinx directory) then right clicking and selecting ``, then type the following command:
`dotnet build -c Release -o build`
the built files will be found in the newly created build directory.

Ryujinx system files are stored in the `Ryujinx` folder. This folder is located in the user folder, which can be accessed by clicking `Open Ryujinx Folder` under the File menu in the GUI.


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

If you are having problems launching homebrew or a particular game marked status-playable or status-ingame in our compatibility list, or if you need help with setting up Ryujinx we recommend you ask questions in the #support channel of our [Discord server](https://discord.gg/Ryujinx) or take a look at our <a href="https://github.com/Ryujinx/Ryujinx/wiki/Frequently-Asked-Questions" target="_blank">FAQ.</a></i><br />  If you have contributions, have suggestions, or just want to get in touch with the team join our [Discord server](https://discord.gg/Ryujinx).


## Donations
                  
If you'd like to support us financially, please take a look at our Patreon

<a href="https://www.patreon.com/ryujinx">
    <img src="https://images.squarespace-cdn.com/content/v1/560c1d39e4b0b4fae0c9cf2a/1567548955044-WVD994WZP76EWF15T0L3/Patreon+Button.png?format=500w" width="150">
</a>

All the developers working on the project do so on their free time, but the project has several expenses:
* Hackable Nintendo Switch consoles to reverse-engineer the hardware
* Additional hardware to test and fix any bugs certain vendors (AMD, Nvidia, and Intel) may have 
* Software licenses to allow developers use whatever they are comfortable with
* Web hosting and infrastructure 
 
All the funds acquired through our Patreon are considered a donation to support our work. Patrons get 1 week of exclusive early access to our progress reports and access to developer interviews.

## License

This software is licensed under the terms of the <a href="https://github.com/Ryujinx/Ryujinx/blob/master/LICENSE.txt" target="_blank">MIT license.</a></i><br /> 
The Ryujinx.Audio project is licensed under the terms of the <a href="https://github.com/Ryujinx/Ryujinx/blob/master/Ryujinx.Audio/LICENSE.txt
" target="_blank">LGPLv3 license.</a></i><br />
This project makes use of code authored by the libvpx project, licensed under BSD and the ffmpeg project, licensed under LGPLv3.
See [LICENSE.txt](LICENSE.txt) and [THIRDPARTY.md](Ryujinx/THIRDPARTY.md) for more details.
## Credits

- [LibHac](https://github.com/Thealexbarney/LibHac) is used for our file-system. 
- [AmiiboAPI](https://www.amiiboapi.com) is used in our Amiibo emulation.
