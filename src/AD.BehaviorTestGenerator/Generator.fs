namespace AD.BehaviorTestGenerator

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

    let private identExpr parts = SynExpr.CreateLongIdent(LongIdentWithDots.Create(parts))

    let appExpr op left right = SynExpr.CreateApp(SynExpr.CreateApp(left, SynExpr.CreateIdent(Ident.Create(op))), right)

    let private pipe left right = appExpr "op_PipeRight" left right

    let private compose left right = appExpr "op_ComposeRight" left right

    let private toTests behaviors =
        match behaviors |> List.choose toTestProperties with
        | [] -> []
        | ps ->
            ps
            |> List.map (fun (name, imp, props) ->
                let testClassAttribute = SynAttribute.Create(["Microsoft"; "VisualStudio"; "TestTools"; "UnitTesting"; "TestClass"] |> List.map Ident.Create, SynConst.Unit)
                let info = { SynComponentInfoRcd.Create([Ident.Create (name + "Test")]) with Attributes = [SynAttributeList.Create(testClassAttribute)]}
                let ctor = SynMemberDefn.CreateImplicitCtor()
                let checkPattern = SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString("check"), [SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString("property"), [])])
                let checkExpr = pipe (compose (identExpr ["property"]) (identExpr ["Async"; "RunSynchronously"])) (identExpr ["FsCheck"; "Check"; "QuickThrowOnFailure"])
                let check = SynMemberDefn.LetBindings ([{ SynBindingRcd.Let with Pattern = checkPattern; ReturnInfo = None; Expr = checkExpr }.FromRcd], false, false, Range.range.Zero)
                
                let behaviorPattern = SynPatRcd.CreateLongIdent(LongIdentWithDots.Create(["_"; "Behavior"]), [])
                let behaviorExpressen =
                    match imp with
                    | Some imp -> pipe (SynExpr.CreateApp(identExpr [imp], SynExpr.CreateUnit)) (identExpr[name])
                    | None -> SynExpr.CreateApp(identExpr [name], SynExpr.CreateUnit)
                let behaviorProp = SynMemberDefn.CreateMember({ SynBindingRcd.Null with Access = Some SynAccess.Private ; Pattern = behaviorPattern; Expr = behaviorExpressen })

                let tests =
                    props
                    |> List.map (fun prop ->
                        let testMethodAttribute = SynAttribute.Create(["Microsoft"; "VisualStudio"; "TestTools"; "UnitTesting"; "TestMethod"] |> List.map Ident.Create, SynConst.Unit)
                        let testMethodName = SynPatRcd.CreateLongIdent(LongIdentWithDots.Create(["test"; prop]), [SynPatRcd.Const({ Const = SynConst.Unit; Range = Range.range.Zero })])
                        let testExpr = pipe (identExpr ["test"; "Behavior"; prop]) (identExpr ["check"])

                        SynMemberDefn.CreateMember({ SynBindingRcd.Null with Attributes = [SynAttributeList.Create(testMethodAttribute)] ; Pattern = testMethodName; Expr = testExpr }))

                SynModuleDecl.CreateType(info, [ctor; check ; behaviorProp] @ tests))

    let private createTests (namespace', behaviors) = behaviors |> toTests |> (AstRcd.SynModuleOrNamespaceRcd.CreateNamespace namespace').AddDeclarations

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
