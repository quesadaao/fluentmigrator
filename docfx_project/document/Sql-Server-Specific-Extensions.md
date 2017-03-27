FluentMigrator supports some extra functions that are specific to Sql Server only. 

## Adding the FluentMigrator.Runner assembly as a reference

These extension methods are not included in the core dll so to get access them you have to include FluentMigrator.Runner.dll as a reference in your migrations project. If you have downloaded the FluentMigrator.Tools Nuget package that contains all the different configurations of FluentMigrator then you have to locate the correct dll (an example would be if you have a .NET 4.0 project on the x64 platform then the dll is located in the packages\FluentMigrator.Tools.1.0.3.0\tools\AnyCPU\40 folder). And the final step is to add the following using to your migration class:
```cs
using FluentMigrator.Runner.Extensions;
```
## Constraint Clustering (Clustered or NonClustered)

This extension allows you to specify a primary key or unique constraint as clustered or non-clustered.
SQL Server tries do the following, unless you specify 'clustered' or 'nonclustered' in sql:
* Create a primary key with a 'clustered' index
* Create a unique constraint with a 'nonclustered' index

Therefore, the most common usage pattern is likely to be along the lines of:

### Create a primary key with a 'nonclustered' index
```cs
Create.PrimaryKey("PK").OnTable("TestTable").Column("Id").NonClustered();
```
### Create a unique constraint with a 'clustered' index
```cs
Create.UniqueConstraint("UQ").OnTable("TestTable").Column("Name").Clustered();
```
Note: You have to create the primary key index or unique constraint separately from the Create.Table expression to be able to specify them as clustered or non-clustered.

## WithIdentityInsert

If you want to turn on Identity Insert to be able to insert values into an identity column (see [here](http://msdn.microsoft.com/en-us/library/ms188059.aspx) for more details) then FluentMigrator has an extension method that supports this.
```cs
Insert.IntoTable("Foo")
  .WithIdentityInsert()
  .Row(new { id = 1, name = "Foo 1" });
```
## Add Identity Column with Seed and Increment

If you want to create an identity column and specify the seed (the id to start with) and an increment (how much the id value should increase when inserting new rows) then use this extension method:
```cs
Create.Table("TestTable")
  .WithColumn("Id").AsInt32().Identity(100,1)
```
## Insert data as an Explicit Unicode String

If you want to explicitly insert a string as Unicode then use the ExplicitUnicodeString class:
```cs
Insert.IntoTable("TestTable").Row(new { Name = new ExplicitUnicodeString("T平仮名") });
```