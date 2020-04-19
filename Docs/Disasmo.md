### Disasmo

#### What is Disasmo?
Disasmo is a Visual Studio plug-in that allows you to look at the dissassembly dump from the .NET JIT.

#### How do I use it?

##### Getting the plug-in

Search for it in the plug-ins in Visual Studio, or get it [from the Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=EgorBogatov.Disasmo).

##### Setting up a local CoreCLR
*Note: These instructions are for .NET Core 3.1.3. They should be updated when the project is migrated to .NET 5 upon its release.*

From within the Visual Studio Command Prompt, check out the `v3.1.3` tag from the `dotnet/coreclr` repository: `https://github.com/dotnet/coreclr`, for instance, with `git clone --branch v3.1.3 --depth=1 https://github.com/dotnet/coreclr`. Then, change into the `src` directory, and run `build -checked`. Once it is done building, you will need to provide Disasmo with the path to the now-build runtime upon its first use.
