module internal AD.BehaviorTestGenerator.Behavior

open System
open FSharp.Compiler.SyntaxTree
open FsAst
open Myriad.Core

let private typesFromFile = Ast.fromFilename >> Async.RunSynchronously >> Array.head >> fst >> Ast.extractTypeDefn

let private name (type' : SynTypeDefn) = type'.ToRcd.Info.Id.Head.idText

let private containsBehavior (str : string) = str.IndexOf("Behavior", StringComparison.InvariantCultureIgnoreCase) > -1

let private isBehavior = name >> containsBehavior

let private behaviors (namespace', types) =
    let behavior type' = if type' |> isBehavior then Some type' else None
    match types |> List.choose behavior with
    | [] -> None
    | behaviors -> Some (namespace', behaviors)

let fromFile = typesFromFile >> List.choose behaviors
