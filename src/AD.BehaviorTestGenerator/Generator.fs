namespace AD.BehaviorTestGenerator

open Myriad.Core

[<MyriadGenerator (Config.name)>]
type Generator () =

    let readConfig getter =
        Config.name
        |> getter
        |> Seq.choose (fun (key, value : obj) ->
            if key = Config.classAttribute || key = Config.methodAttribute then
                match value with
                | :? string as value -> Some (key, value)
                | _ -> None
            else None)
        |> Map.ofSeq

    interface IMyriadGenerator with

        member _.ValidInputExtensions = seq { ".fs" }

        member _.Generate (context : GeneratorContext) =
#if DEBUG
            if not System.Diagnostics.Debugger.IsAttached then
                System.Diagnostics.Debugger.Launch() |> ignore
#endif
            let config = context.ConfigGetter |> readConfig
            context.InputFilename |> Behavior.fromFile |> List.map (Test.create config)
