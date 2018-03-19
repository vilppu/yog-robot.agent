namespace YogRobot

module Agent = 
    open System
    open System.Net.Http
    open Newtonsoft.Json
    
    let private getBaseUrl() = Environment.GetEnvironmentVariable("YOG_BOT_BASE_URL")
    let private httpClient = new HttpClient(BaseAddress = Uri(getBaseUrl()))
    
    let PostWithMasterKey (key : MasterKeyToken) (url : string) data = 
        let (MasterKeyToken key) = key
        let json = JsonConvert.SerializeObject data
        async { 
            use content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            content.Headers.Add("yog-robot-key", key)
            let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
            return response |> Http.FailOnServerError
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
            return response |> Http.FailOnServerError
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
            return response |> Http.FailOnServerError
        }
    
    let GetWithMasterKey (key : MasterKeyToken) (url : string) = 
        let (MasterKeyToken key) = key
        async { 
            use request = new HttpRequestMessage(HttpMethod.Get, url)
            request.Headers.Add("Accept", "application/json")
            request.Headers.Add("yog-robot-key", key)
            let! response = httpClient.SendAsync(request) |> Async.AwaitTask
            return response |> Http.FailOnServerError
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
            return response |> Http.FailOnServerError
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
            return response |> Http.FailOnServerError
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
            return response |> Http.FailOnServerError
        }