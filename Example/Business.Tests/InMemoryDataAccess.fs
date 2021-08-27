namespace Business.Tests

open System.Collections.Generic
open Business

type InMemoryDataAccess () =

    let store = Dictionary ()

    member _.Insert : ``insert project entity`` =
        fun projectEntity -> async {
            projectEntity |> store.Add
        }

    member _.Load : ``load project entity`` =
        fun id -> async {
            match id |> store.TryGetValue with
            | (true, project) -> return Some project
            | _ -> return None
        }
