# Switch to Postgres

Details concerning a changeover from SQLite to Postgres. 

## 0. Motivation

- In many prototypical scenarios, multiple apps connect to a common database via loopback or over a network. Beyond the scope of SQLite. 
- Most databases will be closer to Postgres than to SQLite in how they are used. 
- Good first scenario for learning how to use Docker Compose to start multiple containers. 

## 1. Migrations

EF core migrations are created for a specific type of database. A switch means starting from zero when it comes to migrations. 

If we had been in production, we would likely have had to create two named database contexts side by side and use two different sets of migrations in order to successfully and correctly transfer stored data from one to another. 

The "simple" way to do such a thing in code could be to extend the main database context class by a new class for the different database, then create new migrations for that class. If that scenario arises, read this for more information: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli

## 2. Data types

SQLite has a very simple and dynamic type system. It stores `NULL`, `INTEGER`, `REAL`(float64), `TEXT` or `BLOB`. 

Most other databases including Postgres have more advanced type systems, that are static for each column in a table. 

Most of this is paved over by EF Core packages for the different types of database, but you may still see caveats or issues with your app arise when changing over from one to another. 

In our case: 

1. Counters for the database to create primary key IDs for new objects work differently in Postgres compared to SQLite, in a way that arguably is less forgiving and requires us to run additional SQL queries to set these counters correctly. 
2. Timestamps and timestamps with time zones have dedicated types in Postgres. The EF core library for this database type requires us to set the timestamp kind to UTC. 

With these efforts put in, we could successfully migrate from SQLite to Postgres. 
