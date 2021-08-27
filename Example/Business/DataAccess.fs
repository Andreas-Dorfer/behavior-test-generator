namespace Business

type ``insert project entity`` = ProjectId * Project -> Async<unit>

type ``load project entity`` = ProjectId -> Async<Project option>
