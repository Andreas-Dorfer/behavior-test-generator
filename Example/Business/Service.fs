[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module Business.Project
    
open System

let create (insert : ``insert project entity``) : ``create project`` =
    fun project -> async {
        let id = Guid.NewGuid() |> ProjectId
        do! (id, project) |> insert
        return id
    }

let get (laod : ``load project entity``) : ``get project`` =
    fun id -> async {
        return! id |> laod
    }
