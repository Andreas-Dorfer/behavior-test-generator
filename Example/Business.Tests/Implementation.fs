namespace Business.Tests

open Business

type Implementation () =
    
    let dataAccess = new InMemoryDataAccess ()

    member _.Create : ``create project`` = Project.create dataAccess.Insert
    member _.Get : ``get project`` = Project.get dataAccess.Load
