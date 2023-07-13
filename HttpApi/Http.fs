namespace YogRobot

module Http =
    open System.Net.Http

    let private httpClient = new HttpClient()

    let Send (request: HttpRequestMessage) : Async<HttpResponseMessage> =
        async {
            let! response = httpClient.SendAsync request |> Async.AwaitTask
            return response
        }
