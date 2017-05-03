namespace YogRobot

[<AutoOpen>]
module PushNotificationSubscriptionQuery =
    open System
    open System.Collections.Generic
    open System.Threading.Tasks
    open Microsoft.FSharp.Reflection
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    
    let ReadPushNotificationSubscriptions (deviceGroupId : DeviceGroupId) : Task<List<String>> = 
        let collection = PushNotificationSubscriptionCollection
        let deviceGroupId = deviceGroupId.AsString
        let subscriptions = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
        subscriptions.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>()
        |> Then.Map (fun subscriptionsForDeviceGroup ->
            let result =
                if subscriptionsForDeviceGroup :> obj |> isNull then new List<String>()
                else subscriptionsForDeviceGroup.Tokens
            result)
