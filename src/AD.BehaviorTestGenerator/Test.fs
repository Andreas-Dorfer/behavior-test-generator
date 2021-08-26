module internal AD.BehaviorTestGenerator.Test

open FSharp.Compiler
open FSharp.Compiler.SyntaxTree
open FsAst

let private memberName = function
    | SynMemberDefn.Member (member', _) ->
        match member'.ToRcd.Pattern with
        | SynPatRcd.LongIdent ident -> Some ident.Id.Lid.Tail.Head.idText
        | _ -> None
    | _ -> None

let private implementation (members : SynMemberDefns) =
    let type' =
        members |> List.choose (function
            | SynMemberDefn.ImplicitCtor (_, _, SynSimplePats.SimplePats ([SynSimplePat.Typed (_, SynType.LongIdent id, _)], _), _, _, _) -> Some id.AsString
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

let private attribute parts = [SynAttributeList.Create(SynAttribute.Create(parts |> List.map Ident.Create, SynConst.Unit))]

let private identExpr parts = SynExpr.CreateLongIdent(LongIdentWithDots.Create(parts))

let private appExpr op left right = SynExpr.CreateApp(SynExpr.CreateApp(left, SynExpr.CreateIdent(Ident.Create(op))), right)

let private pipe left right = appExpr "op_PipeRight" left right

let private compose left right = appExpr "op_ComposeRight" left right

let private toTests behaviors =
    match behaviors |> List.choose toTestProperties with
    | [] -> []
    | testProperties ->
        testProperties
        |> List.map (fun (name, implementation, properties) ->
            let testClassAttribute =["Microsoft"; "VisualStudio"; "TestTools"; "UnitTesting"; "TestClass"] |> attribute
            let testClassInfo = { SynComponentInfoRcd.Create([Ident.Create (name + "Test")]) with Attributes = testClassAttribute }
            let testClassCtor = SynMemberDefn.CreateImplicitCtor()

            let checkPattern = SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString("check"), [SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString("property"), [])])
            let checkExpr = pipe (compose (identExpr ["property"]) (identExpr ["Async"; "RunSynchronously"])) (identExpr ["FsCheck"; "Check"; "QuickThrowOnFailure"])
            let check = SynMemberDefn.LetBindings ([{ SynBindingRcd.Let with Pattern = checkPattern; ReturnInfo = None; Expr = checkExpr }.FromRcd], false, false, Range.range.Zero)
            
            let behaviorPattern = SynPatRcd.CreateLongIdent(LongIdentWithDots.Create(["_"; "Behavior"]), [])
            let behaviorExpressen =
                match implementation with
                | Some imp -> pipe (SynExpr.CreateApp(identExpr [imp], SynExpr.CreateUnit)) (identExpr[name])
                | None -> SynExpr.CreateApp(identExpr [name], SynExpr.CreateUnit)
            let behaviorMember = SynMemberDefn.CreateMember({ SynBindingRcd.Null with Access = Some SynAccess.Private ; Pattern = behaviorPattern; Expr = behaviorExpressen })

            let testMembers =
                properties
                |> List.map (fun prop ->
                    let testMethodAttribute = ["Microsoft"; "VisualStudio"; "TestTools"; "UnitTesting"; "TestMethod"] |> attribute
                    let testMethodName = SynPatRcd.CreateLongIdent(LongIdentWithDots.Create(["test"; prop]), [SynPatRcd.Const({ Const = SynConst.Unit; Range = Range.range.Zero })])
                    let testExpr = pipe (identExpr ["test"; "Behavior"; prop]) (identExpr ["check"])
                    SynMemberDefn.CreateMember({ SynBindingRcd.Null with Attributes = testMethodAttribute ; Pattern = testMethodName; Expr = testExpr }))

            SynModuleDecl.CreateType(testClassInfo, [testClassCtor; check ; behaviorMember] @ testMembers))

let create (namespace', behaviors) = behaviors |> toTests |> (AstRcd.SynModuleOrNamespaceRcd.CreateNamespace namespace').AddDeclarations
