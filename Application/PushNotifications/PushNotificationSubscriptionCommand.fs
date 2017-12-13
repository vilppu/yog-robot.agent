namespace YogRobot

[<AutoOpen>]
module PushNotificationSubscriptionCommand =
    open System.Collections.Generic
    open System.Linq    
    open MongoDB.Bson
    open MongoDB.Driver
    
    let RemovePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (subscriptions : PushNotificationSubscription list)=
        let deviceGroupId = deviceGroupId.AsString
        let tokens =
            subscriptions
            |> Seq.map (fun subscription -> subscription.Token)        
        let collection = PushNotificationSubscriptionCollection
        let stored = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
        async {
            let! stored = stored.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>() |> Async.AwaitTask
            stored.Tokens.RemoveAll (fun token -> tokens.Contains(token)) |> ignore
            let options = UpdateOptions()
            options.IsUpsert <- true
            return!
                collection.ReplaceOneAsync<StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, options)
                |> Async.AwaitTask
                |> Async.Ignore
        }
    
    let StorePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (subscriptions : PushNotificationSubscription list) =
        async {
            let collection = PushNotificationSubscriptionCollection
            let deviceGroupId = deviceGroupId.AsString
            let stored = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
            let! stored = stored.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>() |> Async.AwaitTask
            let stored =
                if stored :> obj |> isNull then
                    { Id = ObjectId.Empty
                      DeviceGroupId = deviceGroupId
                      Tokens = new List<string>() }
                else stored
            let toBeAdded =
                subscriptions
                |> List.map (fun subscription -> subscription.Token)
                |> List.filter (fun token -> not(stored.Tokens.Contains(token)))
            if not(toBeAdded |> List.isEmpty) then
                do!
                    stored.Tokens.AddRange toBeAdded
                    let options = UpdateOptions()
                    options.IsUpsert <- true    
                    collection.ReplaceOneAsync<StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, options)
                    |> Async.AwaitTask
                    |> Async.Ignore
        }
    
    let StorePushNotificationSubscription (deviceGroupId : DeviceGroupId) (subscription : PushNotificationSubscription) =
        StorePushNotificationSubscriptions deviceGroupId [subscription]