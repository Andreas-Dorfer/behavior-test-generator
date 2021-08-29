namespace AD.BehaviorTestGenerator

open Myriad.Core

[<MyriadGenerator (Config.name)>]
type Generator () =

    interface IMyriadGenerator with

        member _.ValidInputExtensions = seq { ".fs" }

        member _.Generate (context : GeneratorContext) =
#if DEBUG
            if not System.Diagnostics.Debugger.IsAttached then
                System.Diagnostics.Debugger.Launch() |> ignore
#endif
            let config = context.ConfigGetter |> Config.read
            context.InputFilename |> Behavior.fromFile |> List.map (Test.create config)
