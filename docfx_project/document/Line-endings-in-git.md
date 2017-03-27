To avoid problems with line endings we set autocrlf to true in Git.
Merging or rebasing code with lf endings is very unpleasant. To set the
autocrlf option for your local FluentMigrator Git repository, cd (change
directory) to the FluentMigrator folder and use the following command:

    git config core.autocrlf true
