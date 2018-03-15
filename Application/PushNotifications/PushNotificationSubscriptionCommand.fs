namespace YogRobot

module PushNotificationSubscriptionCommand =
    open System.Collections.Generic
    open System.Linq    
    open MongoDB.Bson
    open MongoDB.Driver
    
    let RemovePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (subscriptions : PushNotificationSubscriptions.PushNotificationSubscription list)=
        let deviceGroupId = deviceGroupId.AsString
        let tokens =
            subscriptions
            |> Seq.map (fun subscription -> subscription.Token)        
        let collection = PushNotificationSubscriptionBsonStorage.PushNotificationSubscriptionCollection
        let stored = collection.Find<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
        async {
            let! stored = stored.FirstOrDefaultAsync<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>() |> Async.AwaitTask
            stored.Tokens.RemoveAll (fun token -> tokens.Contains(token)) |> ignore
            let options = UpdateOptions()
            options.IsUpsert <- true
            return!
                collection.ReplaceOneAsync<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, options)
                |> Async.AwaitTask
                |> Async.Ignore
        }
    
    let StorePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (subscriptions : PushNotificationSubscriptions.PushNotificationSubscription list) =
        async {
            let collection = PushNotificationSubscriptionBsonStorage.PushNotificationSubscriptionCollection
            let deviceGroupId = deviceGroupId.AsString
            let stored = collection.Find<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
            let! stored = stored.FirstOrDefaultAsync<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>() |> Async.AwaitTask
            let stored : PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions =
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
                    collection.ReplaceOneAsync<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, options)
                    |> Async.AwaitTask
                    |> Async.Ignore
        }
    
    let StorePushNotificationSubscription (deviceGroupId : DeviceGroupId) (subscription : PushNotificationSubscriptions.PushNotificationSubscription) =
        StorePushNotificationSubscriptions deviceGroupId [subscription]