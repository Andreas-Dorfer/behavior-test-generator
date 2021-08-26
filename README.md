[![NuGet Package](https://img.shields.io/nuget/v/AndreasDorfer.BehaviorTestGenerator.svg)](https://www.nuget.org/packages/AndreasDorfer.BehaviorTestGenerator/)
# AD.BehaviorTestGenerator
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
Then, you can specify the implementation's behavior like this:
```fsharp
type Behavior (imp : Implementation) =

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
To activate `AD.BehaviorTestGenerator`, add the `MyriadFile` element:
```xml
<Compile Include="Behavior.fs" />
<Compile Include="BehaviorTests.fs">
  <MyriadFile>Behavior.fs</MyriadFile>
</Compile>
```
Now, `AD.BehaviorTestGenerator` turns the behavior into a test class:
```fsharp
[<Microsoft.VisualStudio.TestTools.UnitTesting.TestClass>]
type BehaviorTest() =
    let check property =
        property
        >> Async.RunSynchronously
        |> FsCheck.Check.QuickThrowOnFailure

    member private _.Behavior = () |> Implementation |> Behavior

    [<Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod>]
    member test.``create a project``() =
        test.Behavior.``create a project`` |> check

    [<Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod>]
    member test.``getting an unknown project returns None``() =
        test.Behavior.``getting an unknown project returns None``
        |> check
```
