namespace YogRobot

module Program = 
    open System
    open System.Threading
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open SelfHost

    let rec HandleException(ex : Exception) =
        match ex.InnerException with
        | null -> printf "%s" ex.Message
        | _ -> HandleException ex.InnerException
    
    [<EntryPoint>]
    let main argv = 
        try 
            let server = CreateHttpServer()
            server.Wait()
            0
        with
        | :? AggregateException as ex -> 
            printfn "Error:"
            for innerExceptions in ex.InnerExceptions do
                HandleException innerExceptions
            -1
        | ex -> 
            printfn "Error:"
            HandleException ex
            -1
