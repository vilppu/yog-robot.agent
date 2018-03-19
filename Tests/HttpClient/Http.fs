namespace YogRobot

module Http =
    open System
    open System.Net.Http
    open System.Net.Http.Headers
    open Newtonsoft.Json
    
    let private getBaseUrl() = Environment.GetEnvironmentVariable("YOG_BOT_BASE_URL")
    let private httpClient = new HttpClient(BaseAddress = Uri(getBaseUrl()))
    
    let FailOnServerError(response : HttpResponseMessage) : HttpResponseMessage = 
        if (int response.StatusCode) >= 500 then
            failwith (response.StatusCode.ToString())
        else
            response

    let Post (token : string) (url : string) data = 
        let json = JsonConvert.SerializeObject data
        async {            
            use requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            use content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            requestMessage.Content <- content
            requestMessage.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
            let! response = httpClient.SendAsync(requestMessage) |> Async.AwaitTask
            return response |> FailOnServerError
        }
    
    let Get (token : string) (url : string) = 
        async { 
            use requestMessage = new HttpRequestMessage(HttpMethod.Get, url)
            requestMessage.Headers.Add("Accept", "application/json")
            requestMessage.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
            let! response = httpClient.SendAsync(requestMessage) |> Async.AwaitTask
            return response |> FailOnServerError
        }
    
    let ContentOrFail(response : Async<HttpResponseMessage>) : Async<string> = 
        async { 
            let! response = response
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            match response.IsSuccessStatusCode with
            | true -> return content
            | false -> return failwith (response.StatusCode.ToString() + " " + response.ReasonPhrase + ": " + content)
        }
    
    let ThrowExceptionOnFailure(response : Async<HttpResponseMessage>) : Async<unit> = 
        async { 
            let! response = response
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            match response.IsSuccessStatusCode with
            | true -> return ()
            | false -> return failwith (response.StatusCode.ToString() + " " + response.ReasonPhrase + ": " + content)
        }