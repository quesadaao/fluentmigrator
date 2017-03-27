**This page is out of date. The FluentMigrator.Example project no longer
includes the discussed rake file.**

The example project is a class library that contains the migrations, and
a rakefile to wrap the Migrator.Console.exe calls. \
You will need ruby, rubygems and the rake gem installed. Instructions to
do this can be found [here](http://rubyonrails.org/download)

Run

    rake compile

from the command line in the projects root directory. This will \
compile the application and copy the required files over to the example
project \
src/FluentMigrator.Example/tools/FluentMigrator. cd into the example
project and compile it:

    cd src\FluentMigrator.Example

    rake

    rake -T

This will list the available commands

    rake clean
    rake compile
    rake db:delete
    rake db:migrate
    rake db:migrate:up
    rake db:migrate:down
    rake db:rollback

The db:migrate commands can take an optional VERSION parameter

    rake db:migrate VERSION=20090906205342

db:rollback can take an optional STEPS parameter.

    rake db:rollback STEPS=2
