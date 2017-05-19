namespace YogRobot

open System
open System.Net
open System.Net.Http
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc

[<Route("api")>]
type ApiController(httpSend : HttpRequestMessage -> Task<HttpResponseMessage>) = 
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
        let token = MasterKeyToken(GenerateSecureToken())
        Agent.SaveMasterKey token
        |> Then.Map (fun key -> this.Json(key))
    
    [<Route("keys/device-group-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostDeviceGroupKey(deviceGroupId : string) : Task<JsonResult> = 
        let token = DeviceGroupKeyToken(GenerateSecureToken())
        Agent.SaveDeviceGroupKey (DeviceGroupId(deviceGroupId)) token
        |> Then.Map (fun key -> this.Json(key))
    
    [<Route("keys/sensor-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostSensorKey(deviceGroupId : string) : Task<JsonResult> = 
        let token = SensorKeyToken(GenerateSecureToken())
        Agent.SaveSensorKey (DeviceGroupId(deviceGroupId)) token
        |> Then.Map (fun key -> this.Json(key))
    
    
    [<Route("sensor/{sensorId}/name/{sensorName}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.PostSensorName (sensorId : string) (sensorName : string) : Task<unit> = 
        let sensorId = SensorId sensorId
        Agent.SaveSensorName (this.DeviceGroupId) sensorId (sensorName)
    
    
    [<Route("sensors")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorStatuses() = 
        Agent.GetSensorStatuses (this.DeviceGroupId)
    
    [<Route("sensor/{sensorId}/history/")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorHistory (sensorId : string) =
        Agent.GetSensorHistory (this.DeviceGroupId) (SensorId sensorId)
    
    
    [<Route("push-notifications/subscribe/{token}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.SubscribeToPushNotification (token : string) : Task<unit> = 
        let subscription = PushNotificationSubscription token
        Agent.SubscribeToPushNotification (this.DeviceGroupId) subscription
    
    [<Route("sensor-data")>]
    [<HttpPost>]
    member this.PostSensorData([<FromBody>]sensorEvent : SensorData) =
        if BotKeyIsMissing this.Request then
            Task.FromResult(this.StatusCode(StatusCodes.Status401Unauthorized))
        else
            let deviceGroupId = FindBotId this.Request
            let saveSensorData = Agent.SaveSensorData httpSend
            saveSensorData (deviceGroupId) (sensorEvent)
            |> Then.Map (fun () -> this.StatusCode(StatusCodes.Status201Created))
