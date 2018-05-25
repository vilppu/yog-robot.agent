namespace YogRobot

module internal Notification =
    open System
    
    type Subscription =
        { Token : string }

    let Subscription token : Subscription =
        { Token = token }

    type private DevicePushNotification =
        { DeviceId : string
          SensorName : string
          MeasuredProperty : string
          MeasuredValue : obj
          Timestamp : DateTime }
   
    type private PushNotificationReason =
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
            
            let notification : Firebase.FirebaseDeviceNotificationContent =
                { deviceId = pushNotification.DeviceId
                  sensorName = pushNotification.SensorName
                  measuredProperty = pushNotification.MeasuredProperty
                  measuredValue = pushNotification.MeasuredValue
                  timestamp = pushNotification.Timestamp }

            let pushNotificationRequestData : Firebase.FirebasePushNotificationRequestData =
                { deviceNotification = notification }

            let pushNotification : Firebase.FirebasePushNotification =
                { data = pushNotificationRequestData
                  registration_ids = subscriptions }
                  
            let! subsriptionChanges = Firebase.SendFirebaseMessages httpSend subscriptions pushNotification
            do! PushNotificationSubscriptionBsonStorage.RemoveRegistrations deviceGroupId.AsString subsriptionChanges.SubscriptionsToBeRemoved
            do! PushNotificationSubscriptionBsonStorage.AddRegistrations deviceGroupId.AsString subsriptionChanges.SubscriptionsToBeAdded
        }

    let private sendPushNotifications httpSend reason =
        async {
            let eventMeasurement = DataTransferObject.Measurement reason.SensorState.Measurement
            let hasChanged =
                if reason.PreviousMeasurement |> isNull then true
                else eventMeasurement.Value <> reason.PreviousMeasurement
            if hasChanged then
                do! sendFirebasePushNotifications httpSend reason
        }
    
    let StorePushNotificationSubscription (deviceGroupId : DeviceGroupId) (subscription : Subscription) =
        PushNotificationSubscriptionBsonStorage.StorePushNotificationSubscriptions deviceGroupId.AsString [subscription.Token]
    
    let Send httpSend (sensorState : SensorState) previousMeasurement =
        async {               
            let reason : PushNotificationReason =
                { SensorState = sensorState
                  PreviousMeasurement = previousMeasurement}
            sendPushNotifications httpSend reason
            // Do not wait for push notifications to be sent to notification provider.
            // This is to ensure that IoT hub does not need to wait for request to complete 
            // for too long.
            |> Async.Start
        }
  