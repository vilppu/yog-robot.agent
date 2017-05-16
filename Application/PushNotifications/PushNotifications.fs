namespace YogRobot

[<AutoOpen>]
module PushNotification =
    open System
    open System.Collections.Generic
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text    
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization
   
    let private sendPushNotifications (toBeUpdated : StorableSensorStatus) (event : SensorEvent) =
        let sensorName =
            if toBeUpdated :> obj |> isNull then event.SensorId.AsString
            else toBeUpdated.SensorName
        let measurement = StorableMeasurement event.Measurement
        let pushNotification : DevicePushNotification =
            { DeviceId = event.DeviceId.AsString
              SensorName = sensorName
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value }
        pushNotification |> SendFirebaseMessages event.DeviceGroupId 

    let SendPushNotifications (toBeUpdated : StorableSensorStatus) (event : SensorEvent) =
        let measurement = StorableMeasurement event.Measurement
        let hasChanged =
            if toBeUpdated :> obj |> isNull then true
            else measurement.Value <> toBeUpdated.MeasuredValue 
        if hasChanged then
            match event.Measurement with
            | Contact contact ->
                sendPushNotifications toBeUpdated event
                |> Then.AsUnit
            | _ -> Then.Nothing
        else
            Then.Nothing
