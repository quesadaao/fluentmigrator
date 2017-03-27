# Release Notes

Nuget package: http://nuget.org/packages/FluentMigrator/

## Moved to github releases
see https://github.com/schambers/fluentmigrator/releases

## 1.1.1
Released: Wednesday, June 26, 2013

The [1.1.1](https://github.com/schambers/fluentmigrator/issues?milestone=9&state=closed) was a small bugfix/improvement release.

Features:

* Expose connectionstring within a migration [@daniellee](https://github.com/daniellee)
* Autoscript migration [@speccabit](https://github.com/speccabit)
* SqlServer Extension: Indexes with include columns [@matthewdunsdon](https://github.com/matthewdunsdon)
* SqlLiteSchemaDumper [@dsoronda](https://github.com/dsoronda)

Bug fixes:

* Oracle: ALTER COLUMN without changing nullability [@swalters](https://github.com/swalters)
* Oracle: ALTER COLUMN to nullable [@vgrigoriu](https://github.com/vgrigoriu)
* MsBuild Runner: Tags didn't work as they should [@daniellee](https://github.com/daniellee)
* MySql: Remapping Money Type [@tommarien](https://github.com/tommarien)
* PostgreSQL: ALTER COLUMN nullable to not nullable [@daniellee](https://github.com/daniellee)
* SqlServer: ALTER COLUMN WithDefault(SystemMethods.NewGuid()) [@daniellee](https://github.com/daniellee)
* SqlServer: ALTER COLUMN WithDefault(SystemMethods.CurrentDateTime()) [@daniellee](https://github.com/daniellee)

## 1.1.0
Released: Sunday, May 05, 2013

In the [1.1.0](https://github.com/schambers/fluentmigrator/issues?milestone=7&state=closed) release, the major focus has been the refactoring of the MigrationRunner to allow two transaction modes. Up to this release, there has been one mode, Transaction-Per-Session. This means that if two migrations or more migrations are run together during the same task (e.g. migrate up or down) then this occurs in the same transaction and if one of the migrations fails then all the migrations are rolled back. We are introducing a new mode called Transaction-Per-Migration. This mode means that each migration is run in its own transaction and allows selected migrations to be run with no transaction. This allows migrations to be able to do tasks like creating full-text indexes which cannot be run in a transaction.

Features:

* Transaction per migration [@tommarien](https://github.com/tommarien)
* New TransactionBehavior attribute that allows a migration to run with no transaction. Idea and original PR from [@ericklaus](https://github.com/ericklaus), implementation by [@tommarien](https://github.com/tommarien)
* Validation for Migrations. Previously when an error has been made in a migration (like forgetting the table name in the Create.Table expression) an exception has been thrown and the user sees a stacktrace. This will make MigrationRunner return a friendly error instead if there is a validation rule. [@daniellee](https://github.com/daniellee)
* Added transaction handling to Jet [@dabide](https://github.com/dabide)
* Support for [Auto Reversing](https://github.com/schambers/fluentmigrator/wiki/Auto-Reversing-Migrations) of CreateConstraintExpression for Firebird [@concept-hf](https://github.com/concept-hf)
* SQL Server specific - ExplicitUnicodeString class for the Insert expression that prefixes a string with N'Text' allowing the contents to be treated as explicit Unicode. [@Ashthos](https://github.com/Ashthos)

Bug fixes:

* Create.UniqueConstraint for Postgres did not support the InSchema expression. [@tommarien](https://github.com/tommarien)
* Sqlite - Incorrect quoting of identifers causing problems in the where clause. Changed to double quotes instead of single quotes. [@ClockworkPenguin](https://github.com/ClockworkPenguin)
* All custom exceptions deriving from ApplicationException caused problems in WinRT. Fixed to derive from System.Exception instead. [@tommarien](https://github.com/tommarien)
* Split a batched SQL string into separate statements for SQLServerCE [@redsolo](https://github.com/redsolo)
* Sealed the down method on ForwardOnlyMigration [@MatthewLymer](https://github.com/MatthewLymer)
* The FormatDateTime() call in the JetQuoter is was returning "YYYY-02-DD 08:23:15" for 2/14/2013 8:23:15. A change made to lowercase the Ys and Ds, yielding "2013-02-14 08:23:15". [@tgmayfield](https://github.com/tgmayfield)

## 1.0.6

Released: Monday, December 31, 2012

[1.0.6](https://github.com/schambers/fluentmigrator/issues?milestone=6&state=closed) was a release with lots of small bug fixes and improvements.
* More [SystemMethods](https://github.com/schambers/fluentmigrator/wiki/Use-inbuilt-database-functions-when-setting-the-default-value) implemented. CurrentUser for SqlServer and Postgres. CurrentUTCDateTime for Postgres. CurrentDateTime for MySql. [@daniellee](https://github.com/daniellee)
* SqlServerCE now works after some fixes done by [@heing](https://github.com/heing)
* Quoting and case sensitivity issues for Oracle fixed by [@heing](https://github.com/heing) again.
* The default versiontablemetdata is now overridable (see [Overriding the DefaultVersionTableMetaData class](https://github.com/schambers/fluentmigrator/wiki/Create-custom-metadata-for-the-VersionInfo-table) by [@tommarien](https://github.com/tommarien)
* A small fix to the SqlServerSchemaDumper by [@qntmfred](https://github.com/qntmfred)
* A bug fix for SqlServer so that default value constraint names are now escaped. This allows you to have dots in your table names (don't ask me why you'd do that). The fix was done by [@Pathoschild](https://github.com/Pathoschild)
* Added support for byte arrays when inserting data. Change done by [@spaccabit](https://github.com/spaccabit)
* A change to allow using raw sql when using the Insert.IntoTable expression. [Description in the wiki here.](https://github.com/schambers/fluentmigrator/wiki/Raw-Sql-Helper-for-inserting-data). Change done by [@duereg](https://github.com/duereg).