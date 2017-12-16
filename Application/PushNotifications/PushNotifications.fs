namespace YogRobot

[<AutoOpen>]
module PushNotification =
   
    type PushNotificationReason =
        { // Event causing the push notification
          Event : SensorEvent
          // Sensor status after the event
          Status : StorableSensorStatus }

    let private sendPushNotifications httpSend reason =
        let sensorName =
            if reason.Status :> obj |> isNull then reason.Event.SensorId.AsString
            else reason.Status.SensorName
        let measurement = StorableMeasurement reason.Event.Measurement        
        let sendFirebaseMessages = SendFirebaseMessages httpSend reason.Event.DeviceGroupId
        let pushNotification : DevicePushNotification =
            { DeviceId = reason.Event.DeviceId.AsString
              SensorName = sensorName
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              Timestamp = reason.Event.Timestamp }
        pushNotification |> sendFirebaseMessages

    let SendPushNotifications httpSend reason =
        async {
            let measurement = StorableMeasurement reason.Event.Measurement
            let hasChanged =
                if reason.Status :> obj |> isNull then true
                else measurement.Value <> reason.Status.MeasuredValue 
            if hasChanged then
                match reason.Event.Measurement with
                | Contact contact -> do! sendPushNotifications httpSend reason
                | _ -> ()
        }