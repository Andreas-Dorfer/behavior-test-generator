namespace Business.Tests

open System.Collections.Generic
open Business

type Implementation () =
    
    let store = Dictionary ()

    let insert : ``insert project`` =
        fun projectEntity -> async {
            projectEntity |> store.Add
        }
    
    let load : ``load project`` =
        fun id -> async {
            match id |> store.TryGetValue with
            | (true, project) -> return Some project
            | _ -> return None
        }

    member _.Create : ``create project`` = Service.create insert
    member _.Get : ``get project`` = Service.get load
