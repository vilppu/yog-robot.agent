namespace YogRobot

module Agent = 
    open System
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text
    open Newtonsoft.Json
    
    let private getBaseUrl() = Environment.GetEnvironmentVariable("YOG_BOT_BASE_URL")
    let httpClient = new HttpClient(BaseAddress = Uri(getBaseUrl()))
    let TcpPort = 8888
    
    let PostWithMasterKey (key : MasterKeyToken) (url : string) data = 
        let (MasterKeyToken key) = key
        let json = JsonConvert.SerializeObject data
        async { 
            use content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            content.Headers.Add("yog-robot-key", key)
            let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
            return response
        }
    
    let PostWithDeviceGroupKey (key : DeviceGroupKeyToken) (deviceGroupId : DeviceGroupId) (url : string) data = 
        let (DeviceGroupKeyToken key) = key
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        let json = JsonConvert.SerializeObject data
        async { 
            use content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            content.Headers.Add("yog-robot-device-group-key", key)
            content.Headers.Add("yog-robot-device-group-id", deviceGroupId)
            let! response = httpClient.PostAsync(url, data) |> Async.AwaitTask
            return response
        }
    
    let PostWithSensorKey (key : SensorKeyToken) (deviceGroupId : DeviceGroupId) (url : string) data = 
        let (SensorKeyToken key) = key
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        let json = JsonConvert.SerializeObject data
        async { 
            use content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            content.Headers.Add("yog-robot-sensor-data-key", key)
            content.Headers.Add("yog-robot-bot-id", deviceGroupId)
            let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
            return response
        }
    
    let GetWithMasterKey (key : MasterKeyToken) (url : string) = 
        let (MasterKeyToken key) = key
        async { 
            use request = new HttpRequestMessage(HttpMethod.Get, url)
            request.Headers.Add("Accept", "application/json")
            request.Headers.Add("yog-robot-key", key)
            let! response = httpClient.SendAsync(request) |> Async.AwaitTask
            return response
        }
    
    let GetWithDeviceGroupKey (key : DeviceGroupKeyToken) (deviceGroupId : DeviceGroupId) (url : string) = 
        let (DeviceGroupKeyToken key) = key
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        async { 
            use request = new HttpRequestMessage(HttpMethod.Get, url)
            request.Headers.Add("Accept", "application/json")
            request.Headers.Add("yog-robot-device-group-key", key)
            request.Headers.Add("yog-robot-device-group-id", deviceGroupId)
            let! response = httpClient.SendAsync(request) |> Async.AwaitTask
            return response
        }
    
    let GetWithSensorKey (key : SensorKeyToken) (deviceGroupId : DeviceGroupId) (url : string) = 
        let (SensorKeyToken key) = key
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        async { 
            use request = new HttpRequestMessage(HttpMethod.Get, url)
            request.Headers.Add("Accept", "application/json")
            request.Headers.Add("yog-robot-sensor-data-key", key)
            request.Headers.Add("yog-robot-device-group-id", deviceGroupId)
            let! response = httpClient.SendAsync(request) |> Async.AwaitTask
            return response
        }
    
    let GetWithBotKey (key : SensorKeyToken) (deviceGroupId : DeviceGroupId) (url : string) = 
        let (SensorKeyToken key) = key
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        async { 
            use request = new HttpRequestMessage(HttpMethod.Get, url)
            request.Headers.Add("Accept", "application/json")
            request.Headers.Add("yog-robot-sensor-data-key", key)
            request.Headers.Add("yog-robot-bot-id", deviceGroupId)
            let! response = httpClient.SendAsync(request) |> Async.AwaitTask
            return response
        }
    
    let Post (token : string) (url : string) data = 
        let json = JsonConvert.SerializeObject data
        async {            
            use requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            use content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            requestMessage.Content <- content
            requestMessage.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
            let! response = httpClient.SendAsync(requestMessage) |> Async.AwaitTask
            return response
        }
    
    let Get (token : string) (url : string) = 
        async { 
            use requestMessage = new HttpRequestMessage(HttpMethod.Get, url)
            requestMessage.Headers.Add("Accept", "application/json")
            requestMessage.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
            let! response = httpClient.SendAsync(requestMessage) |> Async.AwaitTask
            return response
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
