namespace YogRobot

[<AutoOpen>]
module PushNotification =
   
    type PushNotificationReason =
        { // Event causing the push notification
          Event : SensorEvent
          // Sensor status after the event
          Status : StorableSensorStatus }

    let private sendFirebasePushNotifications httpSend reason =
        async {
            let measurement = StorableMeasurement reason.Event.Measurement
            let sensorName =
                if reason.Status :> obj |> isNull then reason.Event.SensorId.AsString
                else reason.Status.SensorName
            let sendFirebaseMessages = SendFirebaseMessages httpSend reason.Event.DeviceGroupId
            let pushNotification : DevicePushNotification =
                { DeviceId = reason.Event.DeviceId.AsString
                  SensorName = sensorName
                  MeasuredProperty = measurement.Name
                  MeasuredValue = measurement.Value
                  Timestamp = reason.Event.Timestamp }
            do! sendFirebaseMessages pushNotification
        }

    let private sendPushNotificationsAboutChange httpSend reason =
        async {
            let measurement = StorableMeasurement reason.Event.Measurement
            let hasChanged =
                if reason.Status :> obj |> isNull then true
                else measurement.Value <> reason.Status.MeasuredValue
            if hasChanged then
                do! sendFirebasePushNotifications httpSend reason
        }

    let SendPushNotifications httpSend reason =
        async {
            match reason.Event.Measurement with
            | Contact _ -> do! sendPushNotificationsAboutChange httpSend reason
            | PresenceOfWater _ -> do! sendPushNotificationsAboutChange httpSend reason
            | _ -> ()
        }