namespace YogRobot

module Program =
    open System
    open SelfHost

    let rec HandleException (ex: Exception) =
        match ex.InnerException with
        | null -> eprintf "%s" ex.Message
        | _ -> HandleException ex.InnerException

    [<EntryPoint>]
    let main argv =
        try
            let server = CreateHttpServer Http.Send
            server.Wait()
            0
        with
        | :? AggregateException as ex ->
            eprintfn "Error:"

            for innerExceptions in ex.InnerExceptions do
                HandleException innerExceptions

            -1
        | ex ->
            eprintfn "Error:"
            HandleException ex
            -1
