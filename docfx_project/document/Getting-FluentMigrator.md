The most easy way to get Fluent Migrator and keeping up-to-date is from the nuget packages.

NuGet is a Visual Studio extension that makes it easy to install and update open source libraries and tools in Visual Studio. 

You can install it from [here](http://visualstudiogallery.msdn.microsoft.com/en-us/27077b70-9dad-4c64-adcf-c7cf6bc9970c/file/37502/5/NuGet.Tools.signed.vsix)

## Getting Fluent Migrator

Once Nuget is installed, create a new Visual Studio library project for your migrations. Then launch the **package manager console** from Visual Studio:

![Launching Package Manager Console](http://farm7.static.flickr.com/6073/6114189909_0e8629a2e9.jpg)

In the package manager console, type:

    PM> Install-Package FluentMigrator

Also if you would want to run migrations from the **command prompt, nant or msbuild** you need the Tools package:

    PM> Install-Package FluentMigrator.Tools

Now you are ready to create your first [Migration](Migration.md)