namespace AD.BehaviorTestGenerator.Tests

type Behavior (imp : Implementation) =

    member _.``1st behavior`` () = async {
        return imp.True
    }

    member _.``2st behavior`` (a, b) = async {
        return imp.Plus (a, b) = imp.Plus (b, a)
    }
