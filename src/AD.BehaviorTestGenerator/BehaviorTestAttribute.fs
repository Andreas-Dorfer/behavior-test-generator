namespace AD.BehaviorTestGenerator

open System

type Check =
    | Async = 0
    | Sync = 1

[<Sealed; AttributeUsage (AttributeTargets.Class, AllowMultiple = false)>]
type BehaviorTestAttribute (_check : Check) =
    inherit Attribute ()
