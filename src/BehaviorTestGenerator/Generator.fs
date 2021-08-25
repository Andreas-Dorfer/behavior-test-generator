namespace BehaviorTestGenerator

open System
open System.IO
open System.Text
open FSharp.Compiler
open FSharp.Compiler.SyntaxTree
open FsAst
open Myriad.Core

module Generator =

    let private typesFromFile = Ast.fromFilename >> Async.RunSynchronously >> Array.head >> fst >> Ast.extractTypeDefn

    let private typeName (type' : SynTypeDefn) = type'.ToRcd.Info.Id.Head.idText

    let private containsBehavior (str : string) = str.IndexOf("Behavior", StringComparison.InvariantCultureIgnoreCase) > -1

    let private isBehavior = typeName >> containsBehavior

    let private chooseBehaviors (namespace', types) =
        let chooseBehavior type' = if type' |> isBehavior then Some type' else None
        match types |> List.choose chooseBehavior with
        | [] -> None
        | behaviors -> Some (namespace', behaviors)

    let private behaviorsFromFile = typesFromFile >> List.choose chooseBehaviors

    let private memberName = function
        | SynMemberDefn.Member (member', _) ->
            match member'.ToRcd.Pattern with
            | SynPatRcd.LongIdent ident -> Some ident.Id.Lid.Tail.Head.idText
            | _ -> None
        | _ -> None

    let private implementation (members : SynMemberDefns) =
        let type' =
            members |> List.choose (function
                | SynMemberDefn.ImplicitCtor (_, _, args, _, _, _) ->
                    match args with
                    | SynSimplePats.SimplePats ([SynSimplePat.Typed (_, SynType.LongIdent id, _)], _) -> Some id.AsString
                    | _ -> None
                | _ -> None)
        match type' with
        | [imp] -> Some imp
        | _ -> None

    let private memberNames = function
        | SynTypeDefnRepr.ObjectModel (_, members, _) ->
            match members |> List.choose memberName with
            | [] -> None
            | ms ->
                let imp = members |> implementation
                Some (imp, ms)
        | _ -> None

    let private toTestProperties (behavior : SynTypeDefn) =
        let rcd = behavior.ToRcd
        match rcd.Repr |> memberNames with
        | None _ -> None
        | Some (imp, members) ->
            let name = rcd.Info.Id.Head.idText
            Some (name, imp, members)

    let private longName (id : LongIdent) = String.Join(".", id |> List.map (fun i -> i.idText))

    let private testsToFile (namespace', behaviors) =
        let namespaceName = namespace' |> longName
        match behaviors |> List.choose toTestProperties with
        | [] -> None
        | testProperties ->
            let builder = StringBuilder ()
            let append = builder.AppendLine >> ignore
            $"namespace %s{namespaceName}" |> append
            for (name, imp, props) in testProperties do
                "[<Microsoft.VisualStudio.TestTools.UnitTesting.TestClass>]" |> append
                $"type %s{name}Test () =" |> append
                $"    let check property = property >> Async.RunSynchronously |> FsCheck.Check.QuickThrowOnFailure" |> append
                match imp with
                | Some imp -> $"    member private _.Behavior = () |> %s{imp} |> %s{name}" |> append
                | None -> $"    member private _.Behavior = () |> %s{name}" |> append
                for prop in props do
                    "    [<Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod>]" |> append
                    $"    member test.``%s{prop}`` () =" |> append
                    $"        test.Behavior.``%s{prop}`` |> check" |> append
            
            let file = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
            (file, builder.ToString()) |> File.WriteAllText
            Some file

    let private testsFromFile = typesFromFile >> List.map snd >> List.map (fun type' -> SynModuleDecl.Types (type', Range.range.Zero))

    let private toTests behavior =
        match behavior |> testsToFile with
        | None -> []
        | Some file ->
            let tests = file |> testsFromFile
            file |> File.Delete
            tests

    let private createTests (namespace', behaviors) = (namespace', behaviors) |> toTests |> (AstRcd.SynModuleOrNamespaceRcd.CreateNamespace namespace').AddDeclarations

    let testsFromBehaviorFile = behaviorsFromFile >> List.map createTests


open Generator
open System.Diagnostics

[<MyriadGenerator "behaviorTest">]
type Generator () =
    interface IMyriadGenerator with
        member _.ValidInputExtensions = seq { ".fs" }
        member _.Generate(context : GeneratorContext) =
            if not Debugger.IsAttached then
                Debugger.Launch() |> ignore
            context.InputFilename |> testsFromBehaviorFile
