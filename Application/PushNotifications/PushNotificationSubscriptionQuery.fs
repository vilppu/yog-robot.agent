namespace YogRobot

[<AutoOpen>]
module PushNotificationSubscriptionQuery =
    open System
    open System.Collections.Generic
    open MongoDB.Driver
    
    let ReadPushNotificationSubscriptions (deviceGroupId : DeviceGroupId) : Async<List<String>> =         
        async {
            let collection = PushNotificationSubscriptionCollection
            let deviceGroupId = deviceGroupId.AsString
            let subscriptions = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)

            let! subscriptionsForDeviceGroup =
                subscriptions.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>()
                |> Async.AwaitTask

            let result =
                if subscriptionsForDeviceGroup :> obj |> isNull
                then new List<String>()
                else subscriptionsForDeviceGroup.Tokens

            return result
        }
