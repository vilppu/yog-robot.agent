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
            let key : MasterKey = 
                { Token = token
                  ValidThrough = System.DateTime.UtcNow.AddYears(10) }
            do! SecurityCommands.SaveMasterKey key
            return this.Json(key.Token.AsString)
        }
    
    [<Route("keys/device-group-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostDeviceGroupKey(deviceGroupId : string) : Async<JsonResult> = 
        async {
            let token = DeviceGroupKeyToken(GenerateSecureToken())
            let key : DeviceGroupKey = 
                { Token = token
                  DeviceGroupId = DeviceGroupId deviceGroupId
                  ValidThrough = System.DateTime.UtcNow.AddYears(10) }
            do! SecurityCommands.SaveDeviceGroupKey key
            return this.Json(key.Token.AsString)
        }
    
    [<Route("keys/sensor-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostSensorKey(deviceGroupId : string) : Async<JsonResult> = 
        async {
            let token = SensorKeyToken(GenerateSecureToken())
            let key : SensorKey = 
                { Token = token
                  DeviceGroupId = DeviceGroupId deviceGroupId
                  ValidThrough = System.DateTime.UtcNow.AddYears(10) }
            do! SecurityCommands.SaveSensorKey key
            return this.Json(key.Token.AsString)
        }
    
    [<Route("sensor/{sensorId}/name/{sensorName}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.PostSensorName (sensorId : string) (sensorName : string) : Async<unit> = 
        async {    
            let command : ChangeSensorNameCommand =
                { SensorId = SensorId sensorId
                  DeviceGroupId = this.DeviceGroupId
                  SensorName = sensorName}

            do! SensorCommands.ChangeSensorName command
        }    
    
    [<Route("sensors")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorStatuses() : Async<SensorStatusResult list> = 
        async {
            let! statuses = SensorStatusQuery.GetSensorStatuses (this.DeviceGroupId)
            let result = statuses |> Mapping.ToSensorStatusResults
            return result
        }

    [<Route("sensor/{sensorId}/history/")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorHistory (sensorId : string) : Async<SensorHistoryResult> =
        async {
            let! history = SensorHistoryQuery.GetSensorHistory (this.DeviceGroupId) (SensorId sensorId)
            let result = history |> Mapping.ToSensorHistoryResult
            return result
        }
    
    [<Route("push-notifications/subscribe/{token}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.SubscribeToPushNotifications (token : string) : Async<unit> = 
        async {
            let subscription = PushNotificationSubscription token
            let command : SubscribeToPushNotificationsCommand =
                { DeviceGroupId = (this.DeviceGroupId)
                  Subscription = subscription }
            do! PushNotificationCommands.SubscribeToPushNotifications command
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
                let commands = sensorData |> Mapping.ToChangeSensorStateCommands deviceGroupId
                for command in commands do
                    do! SensorCommands.ChangeSensorState httpSend command
                return this.StatusCode(StatusCodes.Status201Created)
        }
