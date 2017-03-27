It is possible to set the default value when creating or altering a column. To just set a value you can use the following fluent syntax:
```cs
    Create.Table("TestTable").WithColumn("Name").AsString().Nullable().WithDefaultValue("test");
```
However, you can take advantage of some database functions to set the default value. The SystemMethods enum contains five database functions:
* NewGuid
* NewSequentialId
* CurrentDateTime
* CurrentUTCDateTime
* CurrentUser

These are specific to each database, for example CurrentDateTime calls the GETDATE() function for Sql Server and the now() function for Postgres. By using WithDefault instead of WithDefaultValue, you can pass in one of the enum values.

    Create.Table("TestTable").WithColumn("Created").AsDateTime().Nullable().WithDefault(SystemMethods.CurrentDateTime);

### Implemented

* Sql Server all 5 functions implemented
* Postgres 4 functions implemented - NewGuid, CurrentUser, CurrentDateTime and CurrentUTCDateTime (Note: need to run the script share/contrib/uuid-ossp.sql to install the uuid_generate4 function for NewGuid to work. For Postgres 9.1 and 9.2 use this command: CREATE EXTENSION "uuid-ossp"; to install the function.)
* Firebird 2 functions implemented - NewGuid and CurrentDateTime
* Sqlite 1 function implemented - CurrentDateTime
* Oracle 1 function implemented - NewGuid
* MySql 1 function implemented - CurrentDateTime
* Jet no functions implemented