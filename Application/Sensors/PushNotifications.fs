namespace YogRobot

module internal PushNotifications =
    open System

    type DevicePushNotification =
        { DeviceId : string
          SensorName : string
          MeasuredProperty : string
          MeasuredValue : obj
          Timestamp : DateTime }
   
    type PushNotificationReason =
        { SensorState : SensorState
          PreviousMeasurement : obj }
    
    let private sendFirebasePushNotifications httpSend reason =
        async {
            let measurement = DataTransferObject.Measurement reason.SensorState.Measurement
            let sensorName = reason.SensorState.SensorId.AsString       
            
            let deviceGroupId = reason.SensorState.DeviceGroupId
            let! subscriptions = PushNotificationSubscriptionBsonStorage.ReadPushNotificationSubscriptions deviceGroupId.AsString

            let pushNotification : DevicePushNotification =
                { DeviceId = reason.SensorState.DeviceId.AsString
                  SensorName = sensorName
                  MeasuredProperty = measurement.Name
                  MeasuredValue = measurement.Value
                  Timestamp = reason.SensorState.LastUpdated }
            
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
            do! PushNotificationSubscriptionBsonStorage.RemoveRegistrations deviceGroupId.AsString subsriptionChanges.SubscriptionsToBeRemoved
            do! PushNotificationSubscriptionBsonStorage.AddRegistrations deviceGroupId.AsString subsriptionChanges.SubscriptionsToBeAdded
        }
    
    let StorePushNotificationSubscription (deviceGroupId : DeviceGroupId) (subscription : PushNotification.Subscription) =
        PushNotificationSubscriptionBsonStorage.StorePushNotificationSubscriptions deviceGroupId.AsString [subscription.Token]

    let SendPushNotifications httpSend reason =
        async {
            let eventMeasurement = DataTransferObject.Measurement reason.SensorState.Measurement
            let hasChanged =
                if reason.PreviousMeasurement |> isNull then true
                else eventMeasurement.Value <> reason.PreviousMeasurement
            if hasChanged then
                do! sendFirebasePushNotifications httpSend reason
        }
