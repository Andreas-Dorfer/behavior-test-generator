namespace Business.Tests

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
