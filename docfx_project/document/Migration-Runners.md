There are multiple different ways to execute migrations using the
runners. The simplest and most straight forward is from the command line
using the migrate.exe tool.

Command Line Runner
-------------------

For a full listing of options simply run migrate.exe from the command
line by itself or check the [Command Line Runner Options](Command-Line-Runner-Options.md).


    Migrate.exe /connection "Data Source=db\db.sqlite;Version=3;" /db sqlite /target your.migrations.dll

NAnt Runner
-----------

See [Nant Runner Options](Nant-Runner-Options.md) for a complete list of options.

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<project name="fluentmigrator" xmlns="http://nant.sf.net/release/0.85/nant.xsd" default="migrate">
 <loadtasks assembly="../../build/FluentMigrator.NAnt.dll" />

<target name="migrate" description="Migrate the database to the latest version">
 <migrate
                database="sqlite"
                connection="Data Source=:memory:;Version=3;New=True;"
            namespace="FluentMigrator.Tests.Integration.Migrations.Interleaved.Pass3"
                target="../../build/FluentMigrator.Tests.dll"
        />
 </target>

<target name="migrate-rollback" description="Migrate the database back one version">
 <migrate
                database="sqlite"
                connection="Data Source=:memory:;Version=3;New=True;"
            namespace="FluentMigrator.Tests.Integration.Migrations.Interleaved.Pass3"
                target="../../build/FluentMigrator.Tests.dll"
            task="rollback"
        />
 </target>

<target name="migrate-rollback-all" description="Migrates the database back to original state prior to applying migrations">
 <migrate
                database="sqlite"
                connection="Data Source=:memory:;Version=3;New=True;"
            namespace="FluentMigrator.Tests.Integration.Migrations.Interleaved.Pass3"
                target="../../build/FluentMigrator.Tests.dll"
            task="rollback:all"
        />
 </target>
</project>
```

MSBuild Runner
--------------

See [MSBuild Runner Options](MSBuild-Runner-Options.md) for a complete list of options.

```xml
<?xml version="1.0"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" DefaultTargets="Migrate">

<UsingTask TaskName="FluentMigrator.MSBuild.Migrate" 
        AssemblyFile="../../build/FluentMigrator.MSBuild.dll"/>

<Target Name="Migrate" >
 <Message Text="Starting FluentMigrator Migration"/>
 <Migrate Database="postgres"
             Connection="Server=127.0.0.1;Port=5432;Database=FluentMigrator;User Id=test;Password=test;"
             Target="../../build/FluentMigrator.Example.dll">
 </Migrate>
 </Target>

<Target Name="MigrateRollback" >
 <Message Text="Starting FluentMigrator Migration Rollback"/>

<Migrate Database="postgres"
             Connection="Server=127.0.0.1;Port=5432;Database=FluentMigrator;User Id=test;Password=test;"
             Target="../../build/FluentMigrator.Example.dll"
             Task="rollback">
 </Migrate>
 </Target>

<Target Name="MigrateRollbackAll" >
 <Message Text="Starting FluentMigrator Migration Rollback All"/>

<Migrate Database="postgres"
             Connection="Server=127.0.0.1;Port=5432;Database=FluentMigrator;User Id=test;Password=test;"
             Target="../../build/FluentMigrator.Example.dll"
         Task="rollback:all">
 </Migrate>
 </Target>

</Project>
```

Rake Runner
-----------

Requires [Albacore](https://github.com/Albacore/Albacore)


      # simplified rails-like commands
      FLUENTMIGRATOR = "C:\\Path\\To\\migrate.exe"
      MIGRATIONASSEMBLY = 'Path\\To\\MyMigrationAssembly.dll'
      CONNECTIONSTRING = "server=\(local\)\;database=MyDb\;Trusted_Connection=Yes"

      require 'albacore'

      namespace :db do

        desc "migrator task"     
        fluentmigrator :migrator, :task do |migrator, args|
          # these could also be defined in a YML file
          raise "ERROR: :task must be defined" if args[:task].nil?
          migrator.command = FLUENTMIGRATOR
          migrator.provider = 'sqlserver2008'
          migrator.target = MIGRATIONASSEMBLY
          migrator.connection = CONNECTIONSTRING
          migrator.verbose = false
          migrator.task = args[:task]
        end

        namespace :rollback do
          desc "Rollback the database one iteration"
          task :default do |migrator|
            Rake::Task["db:migrator"].reenable
            Rake.application.invoke_task("db:migrator[\"rollback\"]")
          end
        end

        task :rollback => "rollback:default"

        namespace :migrate do
        desc "migrate to current version"      
          task :up do |migratecmd|   
            Rake::Task["db:migrator"].reenable
            Rake.application.invoke_task("db:migrator[\"migrate\"]")
          end 

          desc "migrate down"
            task :down do |migratecmd|  
            Rake::Task["db:migrator"].reenable
            Rake.application.invoke_task("db:migrator[\"migrate:down\"]")
          end

          desc "Redo the last migration"
            task :redo => ["db:rollback", "db:migrate"] do |task|
            puts "Redo complete"
          end
        end
        task :migrate => "migrate:up"
      end   

