module Business.Service
    
open System

let create (insert : ``insert project``) : ``create project`` =
    fun project -> async {
        let id = Guid.NewGuid() |> ProjectId
        do! (id, project) |> insert
        return id
    }

let get (laod : ``load project``) : ``get project`` =
    fun id -> async {
        return! id |> laod
    }
