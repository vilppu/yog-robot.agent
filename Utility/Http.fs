namespace YogRobot

module Http = 
    open System
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text    
    open System.Threading.Tasks
    
    let private httpClient = new HttpClient()

    let Send (request : HttpRequestMessage) : Task<HttpResponseMessage> =
        httpClient.SendAsync request


    
