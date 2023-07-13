namespace YogRobot

module internal Notification =
    open System

    type Subscription = { Token: string }

    let Subscription token : Subscription = { Token = token }

    type private DevicePushNotification =
        { DeviceId: string
          SensorName: string
          MeasuredProperty: string
          MeasuredValue: obj
          Timestamp: DateTime }

    type private PushNotificationReason = { SensorState: SensorState }

    let private sendFirebasePushNotifications httpSend reason =
        async {
            let measurement = DataTransferObject.Measurement reason.SensorState.Measurement
            let sensorName = reason.SensorState.SensorName

            let deviceGroupId = reason.SensorState.DeviceGroupId

            let! subscriptions =
                PushNotificationSubscriptionStorage.ReadPushNotificationSubscriptions deviceGroupId.AsString

            let pushNotification: DevicePushNotification =
                { DeviceId = reason.SensorState.DeviceId.AsString
                  SensorName = sensorName
                  MeasuredProperty = measurement.Name
                  MeasuredValue = measurement.Value
                  Timestamp = reason.SensorState.LastUpdated }

            let notification: FirebaseObjects.FirebaseDeviceNotificationContent =
                { deviceId = pushNotification.DeviceId
                  sensorName = pushNotification.SensorName
                  measuredProperty = pushNotification.MeasuredProperty
                  measuredValue = pushNotification.MeasuredValue
                  timestamp = pushNotification.Timestamp }

            let pushNotificationRequestData: FirebaseObjects.FirebasePushNotificationRequestData =
                { deviceNotification = notification }

            let pushNotification: FirebaseObjects.FirebasePushNotification =
                { data = pushNotificationRequestData
                  registration_ids = subscriptions }

            let! subsriptionChanges = Firebase.SendFirebaseMessages httpSend subscriptions pushNotification

            do!
                PushNotificationSubscriptionStorage.RemoveRegistrations
                    deviceGroupId.AsString
                    subsriptionChanges.SubscriptionsToBeRemoved

            do!
                PushNotificationSubscriptionStorage.AddRegistrations
                    deviceGroupId.AsString
                    subsriptionChanges.SubscriptionsToBeAdded
        }

    let private sendPushNotifications httpSend reason =
        async { do! sendFirebasePushNotifications httpSend reason }

    let Send httpSend (sensorState: SensorState) =
        async {
            let reason: PushNotificationReason = { SensorState = sensorState }

            sendPushNotifications httpSend reason
            // Do not wait for push notifications to be sent to notification provider.
            // This is to ensure that IoT hub does not need to wait for request to complete
            // for too long.
            |> Async.Start
        }
