//------------------------------------------------------------------------------
//        This code was generated by myriad.
//        Changes to this file will be lost when the code is regenerated.
//------------------------------------------------------------------------------
namespace AD.BehaviorTestGenerator.xUnitTests

type BehaviorTest() =
    let imp = Implementation()
    let behavior = Behavior imp
    let check property =
        property >> Async.RunSynchronously |> FsCheck.Check.QuickThrowOnFailure

    [<Xunit.Fact>]
    member _.``always true`` () = behavior.``always true`` |> check

    [<Xunit.Fact>]
    member _.``plus is commutative`` () =
        behavior.``plus is commutative`` |> check

    [<Xunit.Fact>]
    member _.``plus is associative`` () =
        behavior.``plus is associative`` |> check

    interface System.IDisposable with
        member _.Dispose() =
            match imp :> obj with
            | :? System.IDisposable as imp -> imp.Dispose()
            | _ -> ()
