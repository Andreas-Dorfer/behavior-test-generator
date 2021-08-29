module internal AD.BehaviorTestGenerator.Config

[<Literal>]
let name = "behaviorTest"

[<Literal>]
let classAttribute = "classAttribute"

[<Literal>]
let methodAttribute = "methodAttribute"

let read getter =
    name
    |> getter
    |> Seq.choose (fun (key, value : obj) ->
        if key = classAttribute || key = methodAttribute then
            match value with
            | :? string as value -> Some (key, value)
            | _ -> None
        else None)
    |> Map.ofSeq
