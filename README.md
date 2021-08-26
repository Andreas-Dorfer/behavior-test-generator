[![NuGet Package](https://img.shields.io/nuget/v/AndreasDorfer.BehaviorTestGenerator.svg)](https://www.nuget.org/packages/AndreasDorfer.BehaviorTestGenerator/)
# behavior-test-generator
A [Myriad](https://github.com/MoiraeSoftware/myriad) plugin to create test classes from behaviors.
## NuGet Package
    PM> Install-Package AndreasDorfer.BehaviorTestGenerator -Version 0.1.3
## Example
Given a definition:
```fsharp
type ProjectId = ProjectId of Guid

type Project = {
    Number : int
    Name : string
}

type ``create project`` = Project -> Async<ProjectId>

type ``get project`` = ProjectId -> Async<Project option>
```
And some implementation:
```fsharp
type Implementation () =
    member _.Create : ``create project`` = //...
    member _.Get : ``get project`` = //...
```
