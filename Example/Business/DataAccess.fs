namespace Business

type ``insert project`` = ProjectId * Project -> Async<unit>

type ``load project`` = ProjectId -> Async<Project option>
