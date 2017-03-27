By implementing the IVersionTableMetaData interface you can change the defaults for the VersionInfo table. The interface exposes six properties:
* SchemaName (default is empty)
* TableName (default is VersionInfo)
* ColumnName (default is Version)
* UniqueIndexName (default is UC_Version)
* DescriptionColumnName - the name of the last migration applied (default is "Description")
* AppliedOnColumnName - the datetime of when the last migration was applied (default is "AppliedOn")

In the same assembly that your migrations are located, create a new class (it has to be public) that implements the IVersionTableMetaData interface and decorate the class with the VersionTableMetaData attribute. FluentMigrator will automatically find this and use it instead of the default settings.

A common use case is changing the default schema so that you can have a migration assembly per schema. 

```c#
using FluentMigrator.VersionTableInfo;

namespace Migrations
{
    [VersionTableMetaData]
    public class VersionTable : IVersionTableMetaData
    {
        public string ColumnName
        {
            get { return "Version"; }
        }

        public string SchemaName
        {
            get { return ""; }
        }

        public string TableName
        {
            get { return "Version2"; }
        }

        public string UniqueIndexName
        {
            get { return "UC_Version"; }
        }

        public virtual string AppliedOnColumnName
        {
            get { return "AppliedOn"; }
        }

        public virtual string DescriptionColumnName
        {
            get { return "Description"; }
        }
    }
}
```

# Overriding the DefaultVersionTableMetaData class

If you want to keep most of the default values and just change one or two of the properties. Then you can create a class that inherits from DefaultVersionTableMetaData and override the property to be changed. Don't forget to add the VersionTableMetaData attribute to the class.

```c#
[VersionTableMetaData]
public class VersionTable : DefaultVersionTableMetaData
{
    public override string ColumnName
    {
        get { return "Version"; }
    }
}
```