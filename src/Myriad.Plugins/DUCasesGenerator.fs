namespace Myriad.Plugins

open FSharp.Compiler.SyntaxTree
open FsAst
open Myriad.Core

module internal CreateDUModule =
    open FSharp.Compiler.Range

    let createToString (parent: LongIdent) (cases: SynUnionCase list) =
        let varIdent = LongIdentWithDots.CreateString "toString"
        let inputIdent = "x"

        let duType =
            LongIdentWithDots.Create (parent |> List.map (fun i -> i.idText))
            |> SynType.CreateLongIdent

        let pattern =
            let ident = Ident(inputIdent, range.Zero)
            let name = SynPatRcd.CreateNamed(ident, SynPatRcd.CreateWild)
            let args = SynPatRcd.CreateTyped(name, duType) |> SynPatRcd.CreateParen
            SynPatRcd.CreateLongIdent(varIdent, [args])

        let expr =
            let matches =
                cases
                |> List.map (fun c ->
                    let case = c.ToRcd
                    let indent = LongIdentWithDots.CreateString (case.Id.idText)
                    let args = if case.HasFields then [SynPatRcd.CreateWild] else []
                    let p = SynPatRcd.CreateLongIdent(indent, args)
                    let rhs =
                       SynExpr.Const(SynConst.CreateString case.Id.idText, range.Zero)
                    SynMatchClause.Clause(p.FromRcd, None, rhs, range.Zero, DebugPointForTarget.Yes )
                )
            let matchOn =
                let ident = LongIdentWithDots.CreateString inputIdent
                SynExpr.CreateLongIdent(false, ident, None)

            SynExpr.Match(DebugPointForBinding.NoDebugPointAtInvisibleBinding , matchOn, matches, range.Zero)

        let returnTypeInfo = SynBindingReturnInfoRcd.Create duType
        SynModuleDecl.CreateLet [{SynBindingRcd.Let with Pattern = pattern; Expr = expr; ReturnInfo = Some returnTypeInfo }]

    let createFromString (parent: LongIdent) (cases: SynUnionCase list) =
        let varIdent = LongIdentWithDots.CreateString "fromString"
        let inputIdent = "x"

        let duType =
            LongIdentWithDots.Create (parent |> List.map (fun i -> i.idText))
            |> SynType.CreateLongIdent

        let inputType =
            LongIdentWithDots.CreateString "string"
            |> SynType.CreateLongIdent

        let pattern =
            let ident = Ident(inputIdent, range.Zero)
            let name = SynPatRcd.CreateNamed(ident, SynPatRcd.CreateWild)
            let args = SynPatRcd.CreateTyped(name, inputType) |> SynPatRcd.CreateParen
            SynPatRcd.CreateLongIdent(varIdent, [args])

        let expr =
            let matches =
                cases
                //Only provide `fromString` for cases with no fields
                |> List.filter (fun c -> not (c.ToRcd.HasFields))
                |> List.map (fun c ->
                    let case = c.ToRcd
                    let rcd = {SynPatConstRcd.Const = SynConst.CreateString case.Id.idText; Range = range.Zero }
                    let p = SynPatRcd.Const (rcd)
                    let rhs =
                        let f = SynExpr.Ident (Ident("Some", range.Zero))
                        let x = SynExpr.Ident (Ident(case.Id.idText, range.Zero))
                        SynExpr.App(ExprAtomicFlag.NonAtomic, false, f, x, range.Zero)
                    SynMatchClause.Clause(p.FromRcd, None, rhs, range.Zero, DebugPointForTarget.No )
                )
            let wildCase =
                let rhs = SynExpr.Ident (Ident("None", range.Zero))
                SynMatchClause.Clause(SynPat.Wild (range.Zero), None, rhs, range.Zero, DebugPointForTarget.No)

            let matchOn =
                let ident = LongIdentWithDots.CreateString inputIdent
                SynExpr.CreateLongIdent(false, ident, None)

            SynExpr.Match(DebugPointForBinding.NoDebugPointAtLetBinding, matchOn, [yield! matches; wildCase], range.Zero)

        let returnTypeInfo = SynBindingReturnInfoRcd.Create duType
        SynModuleDecl.CreateLet [{SynBindingRcd.Let with Pattern = pattern; Expr = expr; ReturnInfo = Some returnTypeInfo }]

    let createToTag (parent: LongIdent) (cases: SynUnionCase list) =
        let varIdent = LongIdentWithDots.CreateString "toTag"
        let inputIdent = "x"

        let duType =
            LongIdentWithDots.Create (parent |> List.map (fun i -> i.idText))
            |> SynType.CreateLongIdent

        let pattern =
            let ident = Ident(inputIdent, range.Zero)
            let name = SynPatRcd.CreateNamed(ident, SynPatRcd.CreateWild)
            let args = SynPatRcd.CreateTyped(name, duType) |> SynPatRcd.CreateParen
            SynPatRcd.CreateLongIdent(varIdent, [args])

        let expr =
            let matches =
                cases
                |> List.mapi (fun i c ->
                    let case = c.ToRcd
                    let indent = LongIdentWithDots.CreateString (case.Id.idText)
                    let args = if case.HasFields then [SynPatRcd.CreateWild] else []
                    let p = SynPatRcd.CreateLongIdent(indent, args)
                    let rhs =
                       SynExpr.Const(SynConst.Int32 i, range.Zero)
                    SynMatchClause.Clause(p.FromRcd, None, rhs, range.Zero, DebugPointForTarget.No )
                )
            let matchOn =
                let ident = LongIdentWithDots.CreateString inputIdent
                SynExpr.CreateLongIdent(false, ident, None)

            SynExpr.Match(DebugPointForBinding.NoDebugPointAtLetBinding , matchOn, matches, range.Zero)

        let returnTypeInfo = SynBindingReturnInfoRcd.Create duType
        SynModuleDecl.CreateLet [{SynBindingRcd.Let with Pattern = pattern; Expr = expr; ReturnInfo = Some returnTypeInfo }]

    let createIsCase (parent: LongIdent) (cases: SynUnionCase list) =
        [ for c in cases do
            let case = c.ToRcd
            let varIdent = LongIdentWithDots.CreateString (sprintf "is%s" case.Id.idText)
            let inputIdent = "x"

            let duType =
                LongIdentWithDots.Create (parent |> List.map (fun i -> i.idText))
                |> SynType.CreateLongIdent

            let pattern =
                let ident = Ident(inputIdent, range.Zero)
                let name = SynPatRcd.CreateNamed(ident, SynPatRcd.CreateWild)
                let args = SynPatRcd.CreateTyped(name, duType) |> SynPatRcd.CreateParen
                SynPatRcd.CreateLongIdent(varIdent, [args])

            let expr =
                let matchCase =
                    let indent = LongIdentWithDots.CreateString (case.Id.idText)
                    let args = if case.HasFields then [SynPatRcd.CreateWild] else []
                    let p = SynPatRcd.CreateLongIdent(indent, args)

                    let rhs = SynExpr.Const (SynConst.Bool true, range.Zero)
                    SynMatchClause.Clause(p.FromRcd, None, rhs, range.Zero, DebugPointForTarget.No)

                let wildCase =
                    let rhs = SynExpr.Const (SynConst.Bool false, range.Zero)
                    SynMatchClause.Clause(SynPat.Wild (range.Zero), None, rhs, range.Zero, DebugPointForTarget.No)

                let matchOn =
                    let ident = LongIdentWithDots.CreateString inputIdent
                    SynExpr.CreateLongIdent(false, ident, None)

                SynExpr.Match(NoDebugPointAtLetBinding, matchOn, [matchCase; wildCase], range.Zero)

            let returnTypeInfo = SynBindingReturnInfoRcd.Create duType
            SynModuleDecl.CreateLet [{SynBindingRcd.Let with Pattern = pattern; Expr = expr; ReturnInfo = Some returnTypeInfo }]
        ]


    let createDuModule (namespaceId: LongIdent) (typeDefn: SynTypeDefn) (config: (string * obj) seq) =
        let (TypeDefn(synComponentInfo, synTypeDefnRepr, _members, _range)) = typeDefn
        let (ComponentInfo(_attributes, _typeParams, _constraints, recordId, _doc, _preferPostfix, _access, _range)) = synComponentInfo
        match synTypeDefnRepr with
        | SynTypeDefnRepr.Simple(SynTypeDefnSimpleRepr.Union(_accessibility, cases, _recordRange), _range) ->

            let ident = LongIdentWithDots.Create (namespaceId |> List.map (fun ident -> ident.idText))
            let openTarget = SynOpenDeclTarget.ModuleOrNamespace(ident.Lid, range0)
            let openParent = SynModuleDecl.CreateOpen (openTarget)

            let toString = createToString recordId cases
            let fromString = createFromString recordId cases
            let toTag = createToTag recordId cases
            let isCase = createIsCase recordId cases

            let declarations = [
                openParent
                toString
                fromString
                toTag
                yield! isCase ]

            let info = SynComponentInfoRcd.Create recordId
            let mdl = SynModuleDecl.CreateNestedModule(info, declarations)
            let dusNamespace =
                config
                |> Seq.tryPick (fun (n,v) -> if n = "namespace" then Some (v :?> string) else None  )
                |> Option.defaultValue "UnknownNamespace"
            {SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong dusNamespace)
                with
                    IsRecursive = true
                    Declarations = [mdl] }
        | _ -> failwithf "Not a record type"



[<MyriadGenerator("dus")>]
type DUCasesGenerator() =

    interface IMyriadGenerator with
        member __.ValidInputExtensions = seq {".fs"}
        member __.Generate(_myriadConfigKey, configGetter, inputFile: string) =
            //_myriadConfigKey is not currently used but could be a failover config section to use when the attribute passes no config section, or used as a root config
            let ast =
                Ast.fromFilename inputFile
                |> Async.RunSynchronously
                |> Array.head
                |> fst

            let namespaceAndrecords =
                Ast.extractDU ast
                |> List.choose (fun (ns, types) ->
                    match types |> List.filter (Ast.hasAttribute<Generator.DuCasesAttribute>) with
                    | [] -> None
                    | types -> Some (ns, types))

            let modules =
                namespaceAndrecords
                |> List.collect (fun (ns, dus) ->
                                    dus
                                    |> List.map (fun du -> let config = Generator.getConfigFromAttribute<Generator.DuCasesAttribute> configGetter du
                                                           CreateDUModule.createDuModule ns du config))

            modules