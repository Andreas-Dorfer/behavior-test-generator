namespace AD.BehaviorTestGenerator.Tests

type Behavior () =

    member _.``1st behavior`` () = async {
        return true
    }

    member _.``2st behavior`` (a, b) = async {
        return a + b = b + a
    }
