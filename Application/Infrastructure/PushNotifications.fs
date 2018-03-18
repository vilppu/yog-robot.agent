namespace YogRobot

module PushNotifications =
    open System

    type DevicePushNotification =
        { DeviceId : string
          SensorName : string
          MeasuredProperty : string
          MeasuredValue : obj
          Timestamp : DateTime }
   
    type PushNotificationReason =
        {
          Event : Event.SensorStateChanged          
          SensorStatusBeforeEvent : SensorStatusBsonStorage.StorableSensorStatus }
    
    let private sendFirebasePushNotifications httpSend reason =
        async {
            let measurement = StorableTypes.StorableMeasurement reason.Event.Measurement

            let sensorName =
                if reason.SensorStatusBeforeEvent :> obj |> isNull then reason.Event.SensorId.AsString
                else reason.SensorStatusBeforeEvent.SensorName         
            
            let deviceGroupId = reason.Event.DeviceGroupId
            let! subscriptions = PushNotificationSubscriptionBsonStorage.ReadPushNotificationSubscriptions deviceGroupId

            let pushNotification : DevicePushNotification =
                { DeviceId = reason.Event.DeviceId.AsString
                  SensorName = sensorName
                  MeasuredProperty = measurement.Name
                  MeasuredValue = measurement.Value
                  Timestamp = reason.Event.Timestamp }
            
            let notification : FirebaseApi.FirebaseDeviceNotificationContent =
                { deviceId = pushNotification.DeviceId
                  sensorName = pushNotification.SensorName
                  measuredProperty = pushNotification.MeasuredProperty
                  measuredValue = pushNotification.MeasuredValue
                  timestamp = pushNotification.Timestamp }

            let pushNotificationRequestData : FirebaseApi.FirebasePushNotificationRequestData =
                { deviceNotification = notification }

            let pushNotification : FirebaseApi.FirebasePushNotification =
                { data = pushNotificationRequestData
                  registration_ids = subscriptions }
                  
            let! subsriptionChanges = FirebaseApi.SendFirebaseMessages httpSend subscriptions pushNotification
            do! PushNotificationSubscriptionBsonStorage.RemoveRegistrations deviceGroupId subsriptionChanges.SubscriptionsToBeRemoved
            do! PushNotificationSubscriptionBsonStorage.AddRegistrations deviceGroupId subsriptionChanges.SubscriptionsToBeAdded
        }

    let private sendContactPushNotifications httpSend reason =
        async {
            let measurement = StorableTypes.StorableMeasurement reason.Event.Measurement
            let hasChanged =
                if reason.SensorStatusBeforeEvent :> obj |> isNull then true
                else measurement.Value <> reason.SensorStatusBeforeEvent.MeasuredValue
            if hasChanged then
                do! sendFirebasePushNotifications httpSend reason
        }        

    let private sendPresenceOfWaterPushNotifications httpSend reason =
        async {
            let eventMeasurement = StorableTypes.StorableMeasurement reason.Event.Measurement
            let hasChanged =
                if reason.SensorStatusBeforeEvent :> obj |> isNull then true
                else eventMeasurement.Value <> reason.SensorStatusBeforeEvent.MeasuredValue
            let isPresent =
                match reason.Event.Measurement with
                | Measurement.PresenceOfWater presenceOfWater -> presenceOfWater = Measurement.Present
                | _ -> false
            if (hasChanged && isPresent) then
                do! sendFirebasePushNotifications httpSend reason
        }
    
    let StorePushNotificationSubscription (deviceGroupId : DeviceGroupId) (subscription : PushNotification.Subscription) =
        PushNotificationSubscriptionBsonStorage.StorePushNotificationSubscriptions deviceGroupId [subscription.Token]

    let SendPushNotifications httpSend reason =
        async {
            match reason.Event.Measurement with
            | Measurement.Contact _ -> do! sendContactPushNotifications httpSend reason
            | Measurement.PresenceOfWater _ -> do! sendPresenceOfWaterPushNotifications httpSend reason
            | _ -> ()
        }
