# Ferret
Tactical DDD toolkit for using JasperFx/Marten as state storage for a domain model.

* https://github.com/jasperfx/marten
* http://jasperfx.github.io/marten/

## Getting Started
`Ferret.Specs` project requires a `connection.txt` file that contains a valid postgres connection string, such as

```
Server=127.0.0.1;Port=5432;User Id={user-id};Password={password};Database=ferret-test;
```

## Building Ferret

execute `build.ps1` to build Ferret and execute the tests (Ferret uses the awesome Cake build tool - http://cakebuild.net/)

## Working with Ferret

Ferret is designed to work with some key concepts of Domain Driven Design. 

* Domain Commands - A request for an aggregate to transition its state (may fail or be invalidated)
* Domain Events - A result of a successful aggregate state transition
* Aggregates - A logical boundary for things that can change in a business transaction of a given context. An aggregate can be represented by a single class or by a multitude of classes.
* State - A snapshot of the state transitions for an aggregate
* Managers - The entry point to your domain that receives commands and dispatches them to the appropriate aggregate
* Projections - forthcoming

## Other things to note

* Good design suggests you only allow changes to occur to a single aggregate per transaction. Multiple aggregates can be loaded and can interact, but only one should have its state transitioned.
* It is recommended that no one other than the Aggregate itself take a dependency on the State of the Aggregate. Otherwise changes to State can ripple throughout your system. Instead, systems external to your domain should take a dependency on Domain Events or Projections, and not directly on Aggregate State.
* You will probably find it beneficial to put your Domain Commands and Domain Events into a separate, shareable assembly. This will make it easier for systems external to your domain to construct commands and work with events.
