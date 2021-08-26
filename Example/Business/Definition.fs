namespace Business

open System

type ProjectId = ProjectId of Guid

type Project = {
    Number : int
    Name : string
}

type ``create project`` = Project -> Async<ProjectId>

type ``get project`` = ProjectId -> Async<Project option>
