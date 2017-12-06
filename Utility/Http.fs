namespace YogRobot

module Http = 
    open System.Net.Http
    open System.Threading.Tasks
    
    let private httpClient = new HttpClient()

    let Send (request : HttpRequestMessage) : Task<HttpResponseMessage> =
        httpClient.SendAsync request
