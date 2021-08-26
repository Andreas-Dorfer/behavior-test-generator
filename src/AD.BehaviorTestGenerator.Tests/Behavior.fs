namespace AD.BehaviorTestGenerator.Tests

type Behavior (imp : Implementation) =

    member _.``always true`` () = async {
        return imp.True
    }

    member _.``plus is commutative`` (a, b) = async {
        return imp.Plus (a, b) = imp.Plus (b, a)
    }

    member _.``plus is associative`` (a, b, c) = async {
        return (((a, b) |> imp.Plus, c) |> imp.Plus) = ((a, (b, c) |> imp.Plus) |> imp.Plus)
    }


