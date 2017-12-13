namespace YogRobot

open System.Net.Http
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc

[<Route("api")>]
type ApiController(httpSend : HttpRequestMessage -> Async<HttpResponseMessage>) = 
    inherit Controller()
    member private this.DeviceGroupId = DeviceGroupId(GetDeviceGroupId this.User)
    
    [<Route("secure-token")>]
    [<HttpGet>]
    member this.GetRandomKey() : Task<string> = Task.FromResult(GenerateSecureToken())

    [<Route("tokens/master")>]
    [<HttpGet>]
    member this.GetMasterAccessToken()  =
        if MasterKeyIsMissing this.Request then
            this.StatusCode(StatusCodes.Status401Unauthorized) :> IActionResult
        else
            this.Json(GenerateMasterAccessToken()) :> IActionResult
    
    [<Route("tokens/device-group")>]
    [<HttpGet>]
    member this.GetDeviceGroupAccessToken() = 
        if DeviceGroupKeyIsMissing this.Request then
            this.StatusCode(StatusCodes.Status401Unauthorized) :> IActionResult
        else
            let deviceGroupId = FindDeviceGroupId this.Request
            this.Json(GenerateDeviceGroupAccessToken(deviceGroupId)) :> IActionResult
    
    [<Route("tokens/sensor")>]
    [<HttpGet>]
    member this.GetSensorAccessToken() = 
        if SensorKeyIsMissing this.Request then
            this.StatusCode(StatusCodes.Status401Unauthorized) :> IActionResult
        else
            let deviceGroupId = FindDeviceGroupId this.Request
            this.Json(GenerateSensorAccessToken(deviceGroupId)) :> IActionResult
    
    [<Route("keys/master-keys")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostMasterKey() : Task<JsonResult> = 
        async {
            let token = MasterKeyToken(GenerateSecureToken())
            let! key = Agent.SaveMasterKey token
            return this.Json(key)
        } |> Async.StartAsTask
    
    [<Route("keys/device-group-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostDeviceGroupKey(deviceGroupId : string) : Task<JsonResult> = 
        async {
            let token = DeviceGroupKeyToken(GenerateSecureToken())
            let! key = Agent.SaveDeviceGroupKey (DeviceGroupId(deviceGroupId)) token
            return this.Json(key)
        } |> Async.StartAsTask
    
    [<Route("keys/sensor-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostSensorKey(deviceGroupId : string) : Task<JsonResult> = 
        async {
            let token = SensorKeyToken(GenerateSecureToken())
            let! key = Agent.SaveSensorKey (DeviceGroupId(deviceGroupId)) token
            return this.Json(key)
        } |> Async.StartAsTask
    
    [<Route("sensor/{sensorId}/name/{sensorName}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.PostSensorName (sensorId : string) (sensorName : string) : Task<unit> = 
        async {
            let sensorId = SensorId sensorId
            do! Agent.SaveSensorName (this.DeviceGroupId) sensorId (sensorName)
        } |> Async.StartAsTask
    
    
    [<Route("sensors")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorStatuses() = 
        async {
            return! Agent.GetSensorStatuses (this.DeviceGroupId)
        } |> Async.StartAsTask
    
    [<Route("sensor/{sensorId}/history/")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorHistory (sensorId : string) =
        async {
            return! Agent.GetSensorHistory (this.DeviceGroupId) (SensorId sensorId)
        } |> Async.StartAsTask    
    
    [<Route("push-notifications/subscribe/{token}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.SubscribeToPushNotification (token : string) : Task<unit> = 
        async {
            let subscription = PushNotificationSubscription token
            do! Agent.SubscribeToPushNotification (this.DeviceGroupId) subscription
        } |> Async.StartAsTask
    
    [<Route("sensor-data")>]
    [<HttpPost>]
    member this.PostSensorData([<FromBody>]sensorEvent : SensorData) =
        async {
            if BotKeyIsMissing this.Request then
                return this.StatusCode(StatusCodes.Status401Unauthorized)
            else
                let deviceGroupId = FindBotId this.Request
                let saveSensorData = Agent.SaveSensorData httpSend
                do! saveSensorData (deviceGroupId) (sensorEvent)
                return this.StatusCode(StatusCodes.Status201Created)
        }
