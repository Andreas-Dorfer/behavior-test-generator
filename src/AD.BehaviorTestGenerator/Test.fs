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

let private attribute parts = [SynAttributeList.Create(SynAttribute.Create(parts |> List.ofArray |> List.map Ident.Create, SynConst.Unit))]

let private identExpr parts = SynExpr.CreateLongIdent(LongIdentWithDots.Create(parts))

let private appExpr op left right = SynExpr.CreateApp(SynExpr.CreateApp(left, SynExpr.CreateIdent(Ident.Create(op))), right)

let private pipe left right = appExpr "op_PipeRight" left right

let private compose left right = appExpr "op_ComposeRight" left right

let private toTests config behaviors =
    match behaviors |> List.choose toTestProperties with
    | [] -> []
    | testProperties ->

        let configuredAttribute key =
            config
            |> Map.tryFind key
            |> Option.bind (fun (value : string) -> if value |> String.length > 0 then value.Split '.' |> attribute |> Some else None) |> Option.defaultValue []
        let testClassAttribute = configuredAttribute Config.classAttribute
        let testMethodAttribute = configuredAttribute Config.methodAttribute            

        testProperties
        |> List.map (fun (name, implementation, properties) ->
            let testClassInfo = { SynComponentInfoRcd.Create([Ident.Create (name + "Test")]) with Attributes = testClassAttribute }
            let testClassCtor = SynMemberDefn.CreateImplicitCtor()

            let imp =
                match implementation with
                | Some imp ->
                    let impPattern = SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString("imp"), [])
                    let impExpr = SynExpr.CreateApp(identExpr [imp], SynExpr.CreateUnit)
                    [SynMemberDefn.LetBindings ([{ SynBindingRcd.Let with Pattern = impPattern; ReturnInfo = None; Expr = impExpr }.FromRcd], false, false, Range.range.Zero)]
                | None -> []

            let checkPattern = SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString("check"), [SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString("property"), [])])
            let checkExpr = pipe (compose (identExpr ["property"]) (identExpr ["Async"; "RunSynchronously"])) (identExpr ["FsCheck"; "Check"; "QuickThrowOnFailure"])
            let check = SynMemberDefn.LetBindings ([{ SynBindingRcd.Let with Pattern = checkPattern; ReturnInfo = None; Expr = checkExpr }.FromRcd], false, false, Range.range.Zero)
            
            let behaviorPattern = SynPatRcd.CreateLongIdent(LongIdentWithDots.Create(["_"; "Behavior"]), [])
            let behaviorExpressen =
                match implementation with
                | Some _ -> pipe (identExpr ["imp"]) (identExpr[name])
                | None -> SynExpr.CreateApp(identExpr [name], SynExpr.CreateUnit)
            let behaviorMember = SynMemberDefn.CreateMember({ SynBindingRcd.Null with Access = Some SynAccess.Private ; Pattern = behaviorPattern; Expr = behaviorExpressen })

            let testMembers =
                properties
                |> List.map (fun prop ->
                    let testMethodName = SynPatRcd.CreateLongIdent(LongIdentWithDots.Create(["test"; prop]), [SynPatRcd.Const({ Const = SynConst.Unit; Range = Range.range.Zero })])
                    let testExpr = pipe (identExpr ["test"; "Behavior"; prop]) (identExpr ["check"])
                    SynMemberDefn.CreateMember({ SynBindingRcd.Null with Attributes = testMethodAttribute ; Pattern = testMethodName; Expr = testExpr }))

            let disposeMethodName = SynPatRcd.CreateLongIdent(LongIdentWithDots.Create(["_"; "Dispose"]), [SynPatRcd.Const({ Const = SynConst.Unit; Range = Range.range.Zero })])
            let disposeExpr = SynExpr.CreateMatch (SynExpr.Upcast(identExpr ["imp"], SynType.Create("obj"), Range.range.Zero), [
                    SynMatchClause.Clause(SynPat.Named(SynPat.IsInst(SynType.Create("System.IDisposable"), Range.range.Zero), Ident.Create("imp"), false, None, Range.range.Zero), None, SynExpr.CreateInstanceMethodCall(LongIdentWithDots.Create(["imp"; "Dispose"])), Range.range.Zero,DebugPointForTarget.No)
                    SynMatchClause.Clause(SynPatRcd.CreateWild.FromRcd,None, SynExpr.CreateUnit, Range.range.Zero, DebugPointForTarget.No)
                ])
            let dispose = SynMemberDefn.CreateInterface (SynType.Create "System.IDisposable", Some [SynMemberDefn.CreateMember({ SynBindingRcd.Null with Pattern = disposeMethodName; Expr = disposeExpr})])

            SynModuleDecl.CreateType(testClassInfo, testClassCtor :: imp @ [check; behaviorMember] @ testMembers @ [dispose]))

let create config (namespace', behaviors) = behaviors |> toTests config |> (AstRcd.SynModuleOrNamespaceRcd.CreateNamespace namespace').AddDeclarations
