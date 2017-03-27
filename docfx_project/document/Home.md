## Install from Nuget
To get the .NET 4.0 x86 (32-bit) version of the Runner:
```
Install-Package FluentMigrator
```
or
```
Install-Package FluentMigrator.Tools
```
for the x86, x64 and .NET 3.5, 4.0 versions of the Runner.

The command line exe and the NAnt and MSBuild dlls are located in the nuget package. For example, for the FluentMigrator.Tools package, the Migrate.exe is located in the _.\packages\FluentMigrator.Tools.1.0.6.0\tools\AnyCPU\40_ directory.

<!--## Getting Started

* Check out the tour of FluentMigrator starting with [[Getting FluentMigrator]]
* How to create a [[Migration]]
* Learn about the [[Fluent Interface]]
* [[Profiles]] can be used to seed test data
* And then choose one of the [[Migration Runners]] to run your migrations

## More Features

* [[Use inbuilt database functions when setting the default value]] (SystemMethods)
* [[Sql Server Specific Extensions]]
* [[Raw Sql Helper for inserting data]]
* For some migrations you do not need to provide a down action. See [[Auto Reversing Migrations]] for more.
* If you are a Resharper user then checkout the [[Resharper File Template]]
* [[Transaction Modes for the Migration Runner]]

## Advanced Features and Techniques of FluentMigrator

* [[ApplicationContext: Passing parameters to Migrations]]
* [[Dealing with Multiple Database Types]]
* [[Filter migrations run based on Tags]]
* [[Enforce migration version numbering rules]]
* [[Create custom metadata for the VersionInfo table]]

## Contrib

* [SchemaDump](https://github.com/schambers/fluentmigrator/tree/master/src/FluentMigrator.SchemaDump) and here is a [sample](https://gist.github.com/rebootd/924680#file-sqlschemadumpwriter) using SchemaDump to write migrations. 
* [T4 Template](https://github.com/schambers/fluentmigrator/tree/master/src/FluentMigrator.T4) for creating FM initial schemas. [[Guide to FluentMigrator.T4]]

## Current Release

* [Release Notes](https://github.com/schambers/fluentmigrator/releases)

For the [current release](https://github.com/schambers/fluentmigrator/releases/latest) these are the [[Supported Databases]].

## More Information on FluentMigrator

* [[Community articles]]
* Sean Chambers on the [Herding Code podcast](http://herdingcode.com/?p=233)

## Contributors

* [[Code Formatting]]
* [[Line endings in git]]
* [[ContributorList]]-->