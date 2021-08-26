namespace AD.BehaviorTestGenerator

open Myriad.Core

[<MyriadGenerator "behaviorTest">]
type Generator () =

    interface IMyriadGenerator with

        member _.ValidInputExtensions = seq { ".fs" }

        member _.Generate (context : GeneratorContext) =
#if DEBUG
            if not System.Diagnostics.Debugger.IsAttached then
                System.Diagnostics.Debugger.Launch() |> ignore
#endif
            context.InputFilename |> Behavior.fromFile |> List.map Test.create
