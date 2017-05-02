namespace YogRobot

[<AutoOpen>]
module PushNotificationSubscriptionCommand =
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Threading.Tasks
    open Microsoft.FSharp.Reflection
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    
    let RemovePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (subscriptions : PushNotificationSubscription list)=
        let deviceGroupId = deviceGroupId.AsString
        let tokens =
            subscriptions
            |> List.map (fun subscription -> subscription.Token)        
        let collection = PushNotificationSubscriptionCollection
        let subscriptions = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
        let command =
            subscriptions.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>()
            |> Then (fun subscriptions->
                subscriptions.Tokens.RemoveAll (fun token -> tokens.Contains(token)) |> ignore
                let options = UpdateOptions()
                options.IsUpsert <- true    
                collection.ReplaceOneAsync<StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), subscriptions, options)
                )
        command.Unwrap() :> Task
    
    let StorePushNotificationSubscription (deviceGroupId : DeviceGroupId) (subscription : PushNotificationSubscription)=
        let collection = PushNotificationSubscriptionCollection
        let deviceGroupId = deviceGroupId.AsString
        let subscriptions = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
        let command =
            subscriptions.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>()
            |> Then (fun subscriptions->
                let subscriptions =
                    if subscriptions :> obj |> isNull then
                        { Id = ObjectId.Empty
                          DeviceGroupId = deviceGroupId
                          Tokens = new List<string>() }
                    else subscriptions
                if not(subscriptions.Tokens.Contains(subscription.Token)) then
                    subscriptions.Tokens.Add subscription.Token
                let options = UpdateOptions()
                options.IsUpsert <- true    
                collection.ReplaceOneAsync<StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), subscriptions, options)
                )
        command.Unwrap() :> Task
