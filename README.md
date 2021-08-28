[![NuGet Package](https://img.shields.io/nuget/v/AndreasDorfer.BehaviorTestGenerator.svg)](https://www.nuget.org/packages/AndreasDorfer.BehaviorTestGenerator/)
# AD.BehaviorTestGenerator
A [Myriad](https://github.com/MoiraeSoftware/myriad) plugin to generate test classes from behaviors.
## NuGet Package
    PM> Install-Package AndreasDorfer.BehaviorTestGenerator -Version 0.1.6
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
type Implementation() =
    member _.Create : ``create project`` = //...
    member _.Get : ``get project`` = //...
```
Then, you can specify the implementation's behavior like this:
```fsharp
type Behavior(imp : Implementation) =

    member _.``create a project`` expected = async {
        let! id = expected |> imp.Create
        let! actual = id |> imp.Get
        return actual = Some expected
    }

    member _.``getting an unknown project returns None`` unknownId = async {
        let! actual = unknownId |> imp.Get
        return actual = None
    }
```
Add the `MyriadFile` element to activate `AD.BehaviorTestGenerator`:
```xml
<Compile Include="Behavior.fs" />
<Compile Include="BehaviorTests.fs">
  <MyriadFile>Behavior.fs</MyriadFile>
</Compile>
```
Now, `AD.BehaviorTestGenerator` turns the behavior into a test class:
```fsharp
[<TestClass>]
type BehaviorTest() =
    let imp = Implementation()
    let behavior = Behavior imp
    let check property =
        property >> Async.RunSynchronously |> FsCheck.Check.QuickThrowOnFailure

    [<TestMethod>]
    member _.``create a project`` () =
        behavior.``create a project`` |> check

    [<TestMethod>]
    member _.``getting an unknown project returns None`` () =
        behavior.``getting an unknown project returns None`` |> check

    interface System.IDisposable with
        member _.Dispose() =
            match imp :> obj with
            | :? System.IDisposable as imp -> imp.Dispose()
            | _ -> ()
```
It uses [MSTest](https://github.com/microsoft/testfx) and [FsCheck](https://fscheck.github.io/FsCheck/). You can find the full example [here](https://github.com/Andreas-Dorfer/behavior-test-generator/tree/main/Example).
## Note
`AD.BehaviorTestGenerator` is in an early stage. For now, its convention based and not configurable.
### Conventions
- The behavior class's name must contain "behavior".
- The behavior class must have an empty constructor *or* a constructor with a single **typed** parameter for the implementation instance.
- The implementation class must have an empty constructor.
- The behaviors must be public methods with a single parameter (including unit and tuples) and an async result.
