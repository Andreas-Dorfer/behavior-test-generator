//------------------------------------------------------------------------------
//        This code was generated by myriad.
//        Changes to this file will be lost when the code is regenerated.
//------------------------------------------------------------------------------
namespace Business.Tests

[<Microsoft.VisualStudio.TestTools.UnitTesting.TestClass>]
type BehaviorTest() =
    let imp = Implementation()
    let behavior = Behavior imp
    let check property =
        property >> Async.RunSynchronously |> FsCheck.Check.QuickThrowOnFailure

    [<Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod>]
    member _.``create a project`` () = behavior.``create a project`` |> check

    [<Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod>]
    member _.``getting an unknown project returns None`` () =
        behavior.``getting an unknown project returns None`` |> check

    interface System.IDisposable with
        member _.Dispose() =
            match imp :> obj with
            | :? System.IDisposable as imp -> imp.Dispose()
            | _ -> ()