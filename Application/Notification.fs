namespace YogRobot

open FirebaseAdmin.Messaging
open System.Collections.Generic
open System.Text.Json

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

    let private sendFirebasePushNotifications sendFirebaseMulticastMessages reason =
        task {
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

            let notification =
                dict [ ("deviceNotification", JsonSerializer.Serialize(notification)) ]

            let pushNotification: MulticastMessage = new MulticastMessage()

            pushNotification.Data <- new Dictionary<string, string>(notification)
            pushNotification.Tokens <- subscriptions

            let! subsriptionChanges =
                Firebase.SendFirebaseMessages sendFirebaseMulticastMessages subscriptions pushNotification

            do!
                PushNotificationSubscriptionStorage.RemoveRegistrations
                    deviceGroupId.AsString
                    subsriptionChanges.SubscriptionsToBeRemoved

            do!
                PushNotificationSubscriptionStorage.AddRegistrations
                    deviceGroupId.AsString
                    subsriptionChanges.SubscriptionsToBeAdded
        }

    let private sendPushNotifications sendFirebaseMulticastMessages reason =
        task { do! sendFirebasePushNotifications sendFirebaseMulticastMessages reason }

    let Send sendFirebaseMulticastMessages (sensorState: SensorState) =
        task {
            let reason: PushNotificationReason = { SensorState = sensorState }

            let send = fun () -> sendPushNotifications sendFirebaseMulticastMessages reason
            // Do not wait for push notifications to be sent to notification provider.
            // This is to ensure that IoT hub does not need to wait for request to complete
            // for too long.
            System.Threading.Tasks.Task.Run<unit>(send) |> ignore
        }
