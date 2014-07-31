PostgreSqlMigrationSqlGenerator
===============================

Class to handle Entity Framework 5 migrations with PostgreSQL


Requires
------------

1. Npgsql (you can find it in NuGet)
2. A database already creaded with a new schema called "dbo"
3. [MaxLength(n)] attribute is required on each string properties of models


Installation
------------

1. Change your connection string into the file web.conf like this:

``` 
<connectionStrings>
    <add name="DataContext" 
        connectionString="Server=127.0.0.1;Port=5432;Database=db;UserId=postgres;Password=password;CommandTimeout=20;Preload Reader = true;" 
        providerName="Npgsql" />
</connectionStrings>
```

Installation with NuGet
-----------------------

PM> Install-Package EntityFramework.v5.PostgreSql


What's next
-----------

Support to Entity Framework 6 (2014-07-31 update: support to EF6 is quite done!)
