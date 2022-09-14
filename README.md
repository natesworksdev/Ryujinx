# Ryujinx
![Ryujinx logo](https://raw.githubusercontent.com/Ryujinx/Ryujinx/master/Ryujinx.svg)

A service for Linux/OSX desktop applications. The main idea is to provide a lightweight framework for running Linux applications with high performance.

## Building

First of all you have to install `csharp` and `csharp.net` for your IDE, e.g. `VS 2015`:
```shell
$ VS2015
```

Once installed, you have to build a build project to run `Ryujinx` on an OSX or Linux machine. Then you have to create a folder `Ryujinx` in the root of your project directory with the name `Ryujinx` and copy the following files to the build folder:
```shell
c:\Program Files\Ryujinx\bin\Release\Ryujinx\Release.exe
c:\Program Files\Ryujinx\bin\Release\Ryujinx\Release.lib
c:\Program Files\Ryujinx\bin\Release\Ryujinx\Release.config
c:\Program Files\Ryujinx\bin\Release\Ryujinx\Release\Release.vsl
```
Then make `Ryujinx\bin\Release\Ryujinx\Release.config` executable with the following command:
```shell
$./Ryujinx\bin\Release\Ryujinx\Release.config
```

## Usage

After you have created a folder `Ryujinx` in your project and copied the required files, run `Ryujinx\bin\Release\Ryujinx\Release.exe` and `Ryujinx\bin\Release\Ryujinx\Release.lib` to start the application. The application starts running in the background and is not under your control.

Now run `Ryujinx\bin\Release\Ryujinx\Release.config` to change the configuration of the application.

The application uses the following system configuration to locate the application:
```
system.systemConfiguration {
    type = SystemConfiguration.AppDomain;
    applicationPath = "C:\\Program Files\\Ryujinx\\";
    environmentVariables = false;
    appDomain = new AppDomain("Application");
}
```

Once you are finished, run the application again and you should get the application in the `Ryujinx` folder. You can start applications by using the `Ryujinx\bin\Release\Ryujinx\Release.exe` and `Ryujinx\bin\Release\Ryujinx\Release.lib` command.

## License

Copyright © 2015 Ryujinx

Licensed under the MIT license, for more information please visit [http://en.wikipedia.org/wiki/Software_license_mit](http://en.wikipedia.org/wiki/Software_license_mit).
