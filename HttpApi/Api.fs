namespace YogRobot

open System.Net.Http
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open DataTransferObject
open Microsoft.AspNetCore.Cors
open FirebaseAdmin.Messaging

[<Route("api")>]
type ApiController(sendFirebaseMulticastMessages: MulticastMessage -> Task<BatchResponse>) =
    inherit Controller()
    member private this.DeviceGroupId = GetDeviceGroupId this.User

    [<Route("secure-token")>]
    [<HttpGet>]
    member this.GetRandomKey() : string = Application.GenerateSecureToken()

    [<Route("tokens/master")>]
    [<HttpGet>]
    member this.GetMasterAccessToken() =
        task {
            let! keyIsMissing = MasterKeyIsMissing this.Request

            if keyIsMissing then
                return this.StatusCode(StatusCodes.Status401Unauthorized) :> IActionResult
            else
                return this.Json(GenerateMasterAccessToken()) :> IActionResult
        }

    [<Route("tokens/device-group")>]
    [<HttpGet>]
    member this.GetDeviceGroupAccessToken() =
        task {
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
        task {
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
    member this.PostDeviceGroupKey(deviceGroupId: string) : Task<JsonResult> =
        task {
            let token = Application.GenerateSecureToken()
            let! key = Application.PostDeviceGroupKey sendFirebaseMulticastMessages deviceGroupId token
            return this.Json(key)
        }

    [<Route("keys/sensor-keys/{deviceGroupId}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.Administrator)>]
    member this.PostSensorKey(deviceGroupId: string) : Task<JsonResult> =
        task {
            let token = Application.GenerateSecureToken()
            let! key = Application.PostSensorKey sendFirebaseMulticastMessages deviceGroupId token
            return this.Json(key)
        }

    [<Route("sensor/{sensorId}/name/{sensorName}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.PostSensorName (sensorId: string) (sensorName: string) : Task<unit> =
        task { do! Application.PostSensorName sendFirebaseMulticastMessages this.DeviceGroupId sensorId sensorName }

    [<Route("sensors")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorState() : Task<DataTransferObject.SensorState list> =
        task { return! Application.GetSensorState this.DeviceGroupId }

    [<Route("sensor/{sensorId}/history/")>]
    [<HttpGet>]
    [<Authorize(Policy = Roles.User)>]
    member this.GetSensorHistory(sensorId: string) : Task<DataTransferObject.SensorHistory> =
        task { return! Application.GetSensorHistory this.DeviceGroupId sensorId }

    [<Route("push-notifications/subscribe/{token}")>]
    [<HttpPost>]
    [<Authorize(Policy = Roles.User)>]
    member this.SubscribeToPushNotifications(token: string) : Task<unit> =
        task { return! Application.SubscribeToPushNotifications sendFirebaseMulticastMessages this.DeviceGroupId token }

    [<Route("sensor-data")>]
    [<HttpPost>]
    member this.PostSensorData([<FromBody>] sensorData: SensorData) =
        task {
            printfn "Sensor data posted"

            let! keyIsMissing = SensorKeyIsMissing this.Request

            if keyIsMissing then
                return this.StatusCode(StatusCodes.Status401Unauthorized)
            else
                let deviceGroupId = FindDeviceGroupId this.Request
                do! Application.PostSensorData sendFirebaseMulticastMessages deviceGroupId sensorData
                return this.StatusCode(StatusCodes.Status201Created)
        }
