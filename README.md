InvokeMySqlCmd
==============

Powershell Cmdlet for MySql with similar functionality to the Sql Server cmdlet Invoke-SqlCmd.

## How to install
1. Download the dlls from the [latest release](https://github.com/ctigeek/InvokeMySqlCmd/releases/), or build it yourself from source.
2. Copy the dlls to one of the defined paths in the PSModulePath environment variable: `$env:PSModulePath`.
	See also [this msdn article](http://msdn.microsoft.com/en-us/library/dd878350) to make sure you have your directory structure correct.
3. Import the module by running "Import-Module InvokeMySqlcmd" in your powershell script. If that doesn't work, then you don't have the DLLs in the correct directory.

## Command Parameters

* `-Query`  The SQL query to run. This is required unless InputFile is included. If you are piping a string into this cmdlet, it will go into the Query parameter.
* `-Username` (required) The MySql username to use.
* `-Password` (required) The MySql password for the user.
* `-QueryTimeout` The timeout for the sql command. Default is 30 seconds.
* `-InputFile` The path to a file containing the sql command to run. If you specify this, do not include the Query parameter.
* `-Scalar` (switch) If included, the result will be the value in the first column of the first row returned.
* `-NonQuery` (switch) Include this if you don't expect rows to be returned (insert/update/delete). The value returned is the number of rows affected.
* `-Database` The default schema. This translates to "initial catalog" in the connection string built by the cmdlet.
* `-Server` The name of the server to connect to. This translates to "Data Source" in the connection string. Default is "localhost".

ex:
* `Invoke-MySqlcmd -username myname -password mypass -query "select * from myschema.mytable limit 5;"`

* `Invoke-MySqlcmd -username myname -password mypass -scalar -query "select someColumn from myschema.mytable limit 1;"`

* `Invoke-MySqlcmd -username myname -password mypass -NonQuery -query "update myschema.mytable set someColumn='blah' where id=3;"`

* `"select * from myschema.mytable limit 5;" | Invoke-MySqlcmd -username myname -password mypass`

	
## What's returned
* For normal select queries, an array of System.Data.DataRow. If the query doesn't return anything, that will be an array with 0 elements.
* If you include the Scalar switch, it will be the value in the first column of the first row returned.
* If you include the NonQuery switch, it will return the number of rows affected by the query.
