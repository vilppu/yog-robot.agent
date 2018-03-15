namespace YogRobot

open System.Net.Http
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc

[<Route("api")>]
type ApiController(httpSend : HttpRequestMessage -> Async<HttpResponseMessage>) = 
    inherit Controller()
    member private this.DeviceGroupId = DeviceGroupId(GetDeviceGroupId this.User)
    
    [<Route("secure-token")>]
    [<HttpGet>]
    member this.GetRandomKey() : string = GenerateSecureToken()

    [<Route("tokens/master")>]
    [<HttpGet>]
    member this.GetMasterAccessToken()  =
        async {
            let! keyIsMissing = MasterKeyIsMissing this.Request
            if keyIsMissing then
                return this.StatusCode(StatusCodes.Status401Unauthorized) :> IActionResult
            else
                return this.Json(GenerateMasterAccessToken()) :> IActionResult
        }
    
    [<Route("tokens/device-group")>]
    [<HttpGet>]
    member this.GetDeviceGroupAccessToken() = 
        async {
            let! keyIsMissing = DeviceGroupKeyIsMissing this.Request
            if keyIsMissing then
                return this.StatusCode(StatusCodes.Status401Unauthorized) :> IActionResult
            else
                let deviceGroupId = FindDeviceGroupId this.Request
                return this.Json(GenerateDeviceGroupAccessToken(deviceGroupId)) :> IActionResult
        }

    [<Route("tokens/sensor")>]
    [<HttpGet>]
    member this.GetSensorAccessToken() = 
        async {
            let! keyIsMissing = SensorKeyIsMissing this.Request
            if keyIsMissing then
                return this.StatusCode(StatusCodes.Status401Unauthorized) :> IActionResult
            else
                let deviceGroupId = FindDeviceGroupId this.Request
                return this.Json(GenerateSensorAccessToken(deviceGroupId)) :> IActionResult
        }
    [<Route("keys/master-keys")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostMasterKey() : Async<JsonResult> = 
        async {
            let token = MasterKeyToken(GenerateSecureToken())
            let! key = Agent.SaveMasterKey token
            return this.Json(key)
        }
    
    [<Route("keys/device-group-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostDeviceGroupKey(deviceGroupId : string) : Async<JsonResult> = 
        async {
            let token = DeviceGroupKeyToken(GenerateSecureToken())
            let! key = Agent.SaveDeviceGroupKey (DeviceGroupId(deviceGroupId)) token
            return this.Json(key)
        }
    
    [<Route("keys/sensor-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostSensorKey(deviceGroupId : string) : Async<JsonResult> = 
        async {
            let token = SensorKeyToken(GenerateSecureToken())
            let! key = Agent.SaveSensorKey (DeviceGroupId(deviceGroupId)) token
            return this.Json(key)
        }
    
    [<Route("sensor/{sensorId}/name/{sensorName}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.PostSensorName (sensorId : string) (sensorName : string) : Async<unit> = 
        async {
            let sensorId = SensorId sensorId
            do! Agent.SaveSensorName (this.DeviceGroupId) sensorId (sensorName)
        }
    
    
    [<Route("sensors")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorStatuses() = 
        async {
            return! Agent.GetSensorStatuses (this.DeviceGroupId)
        }
    
    [<Route("sensor/{sensorId}/history/")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorHistory (sensorId : string) =
        async {
            return! Agent.GetSensorHistory (this.DeviceGroupId) (SensorId sensorId)
        }    
    
    [<Route("push-notifications/subscribe/{token}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.SubscribeToPushNotification (token : string) : Async<unit> = 
        async {
            let subscription = PushNotificationSubscriptions.PushNotificationSubscription token
            do! Agent.SubscribeToPushNotification (this.DeviceGroupId) subscription
        }
    
    [<Route("sensor-data")>]
    [<HttpPost>]
    member this.PostSensorData([<FromBody>]sensorData : SensorData) =
        async {  
            let! keyIsMissing = SensorKeyIsMissing this.Request           
            if keyIsMissing then
                return this.StatusCode(StatusCodes.Status401Unauthorized)
            else
                let deviceGroupId = FindDeviceGroupId this.Request
                do! Agent.SaveSensorData httpSend (deviceGroupId) (sensorData)
                return this.StatusCode(StatusCodes.Status201Created)
        }
