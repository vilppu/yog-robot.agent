namespace YogRobot

open System.Net.Http
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open DataTransferObject

[<Route("api")>]
type ApiController(httpSend : HttpRequestMessage -> Async<HttpResponseMessage>) = 
    inherit Controller()
    member private this.DeviceGroupId = GetDeviceGroupId this.User
    
    [<Route("secure-token")>]
    [<HttpGet>]
    member this.GetRandomKey() : string = Application.GenerateSecureToken()

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
    
    [<Route("keys/device-group-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostDeviceGroupKey(deviceGroupId : string) : Async<JsonResult> = 
        async {
            let token = Application.GenerateSecureToken()
            let! key = Application.PostDeviceGroupKey httpSend deviceGroupId token
            return this.Json(key)
        }
    
    [<Route("keys/sensor-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostSensorKey(deviceGroupId : string) : Async<JsonResult> = 
        async {
            let token = Application.GenerateSecureToken()
            let! key = Application.PostSensorKey httpSend deviceGroupId token
            return this.Json(key)
        }
    
    [<Route("sensor/{sensorId}/name/{sensorName}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.PostSensorName (sensorId : string) (sensorName : string) : Async<unit> = 
        async {
            do! Application.PostSensorName httpSend this.DeviceGroupId sensorId sensorName
        }    
    
    [<Route("sensors")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorState() : Async<DataTransferObject.SensorState list> = 
        async {
            return! Application.GetSensorState this.DeviceGroupId
        }

    [<Route("sensor/{sensorId}/history/")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorHistory (sensorId : string) : Async<DataTransferObject.SensorHistory> =
        async {
            return! Application.GetSensorHistory this.DeviceGroupId sensorId
        }
    
    [<Route("push-notifications/subscribe/{token}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.SubscribeToPushNotifications (token : string) : Async<unit> = 
        async {
            return! Application.SubscribeToPushNotifications httpSend this.DeviceGroupId token
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
                return! Application.PostSensorData httpSend deviceGroupId sensorData
                return this.StatusCode(StatusCodes.Status201Created)
        }
