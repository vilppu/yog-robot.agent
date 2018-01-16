namespace YogRobot

[<AutoOpen>]
module PushNotification =
   
    type PushNotificationReason =
        {
          Event : SensorEvent          
          SensorStatusBeforeEvent : StorableSensorStatus }

    let private sendFirebasePushNotifications httpSend reason =
        async {
            let measurement = StorableMeasurement reason.Event.Measurement
            let sensorName =
                if reason.SensorStatusBeforeEvent :> obj |> isNull then reason.Event.SensorId.AsString
                else reason.SensorStatusBeforeEvent.SensorName
            let sendFirebaseMessages = SendFirebaseMessages httpSend reason.Event.DeviceGroupId
            let pushNotification : DevicePushNotification =
                { DeviceId = reason.Event.DeviceId.AsString
                  SensorName = sensorName
                  MeasuredProperty = measurement.Name
                  MeasuredValue = measurement.Value
                  Timestamp = reason.Event.Timestamp }
            do! sendFirebaseMessages pushNotification
        }

    let private sendContactPushNotifications httpSend reason =
        async {
            let measurement = StorableMeasurement reason.Event.Measurement
            let hasChanged =
                if reason.SensorStatusBeforeEvent :> obj |> isNull then true
                else measurement.Value <> reason.SensorStatusBeforeEvent.MeasuredValue
            if hasChanged then
                do! sendFirebasePushNotifications httpSend reason
        }        

    let private sendPresenceOfWaterPushNotifications httpSend reason =
        async {
            let eventMeasurement = StorableMeasurement reason.Event.Measurement
            let hasChanged =
                if reason.SensorStatusBeforeEvent :> obj |> isNull then true
                else eventMeasurement.Value <> reason.SensorStatusBeforeEvent.MeasuredValue
            let isPresent =
                match reason.Event.Measurement with
                | PresenceOfWater presenceOfWater -> presenceOfWater = PresenceOfWater.Present
                | _ -> false
            if (hasChanged && isPresent) then
                do! sendFirebasePushNotifications httpSend reason
        }

    let SendPushNotifications httpSend reason =
        async {
            match reason.Event.Measurement with
            | Contact _ -> do! sendContactPushNotifications httpSend reason
            | PresenceOfWater _ -> do! sendPresenceOfWaterPushNotifications httpSend reason
            | _ -> ()
        }