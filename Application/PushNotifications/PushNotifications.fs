namespace YogRobot

[<AutoOpen>]
module PushNotification =
    open System
    open System.Collections.Generic
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text    
    open System.Threading.Tasks    
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization
   
    let private sendPushNotifications (sendFirebaseMessages : DevicePushNotification -> Task<unit>) (sensorStatus : StorableSensorStatus) (event : SensorEvent) =
        let sensorName =
            if sensorStatus :> obj |> isNull then event.SensorId.AsString
            else sensorStatus.SensorName
        let measurement = StorableMeasurement event.Measurement
        let pushNotification : DevicePushNotification =
            { DeviceId = event.DeviceId.AsString
              SensorName = sensorName
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value }
        pushNotification |> sendFirebaseMessages

    let SendPushNotifications sendFirebaseMessages (sensorStatus : StorableSensorStatus) (event : SensorEvent) =
        let measurement = StorableMeasurement event.Measurement
        let hasChanged =
            if sensorStatus :> obj |> isNull then true
            else measurement.Value <> sensorStatus.MeasuredValue 
        if hasChanged then
            match event.Measurement with
            | Contact contact ->
                sendPushNotifications sendFirebaseMessages sensorStatus event
                |> Then.AsUnit
            | _ -> Then.Nothing
        else
            Then.Nothing
