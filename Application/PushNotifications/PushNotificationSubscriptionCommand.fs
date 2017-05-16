namespace YogRobot

[<AutoOpen>]
module PushNotificationSubscriptionCommand =
    open System
    open System.Collections.Generic
    open System.Linq    
    open Microsoft.FSharp.Reflection
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    
    let RemovePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (subscriptions : PushNotificationSubscription list)=
        let deviceGroupId = deviceGroupId.AsString
        let tokens =
            subscriptions
            |> Seq.map (fun subscription -> subscription.Token)        
        let collection = PushNotificationSubscriptionCollection
        let stored = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
        let command =
            stored.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>()
            |> Promise.FromTask
            |> Promise.Then (fun stored->
                stored.Tokens.RemoveAll (fun token -> tokens.Contains(token)) |> ignore
                let options = UpdateOptions()
                options.IsUpsert <- true
                collection.ReplaceOneAsync<StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, options)
                )
            |> Promise.Ignore
        command
    
    let StorePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (subscriptions : PushNotificationSubscription list)=
        let collection = PushNotificationSubscriptionCollection
        let deviceGroupId = deviceGroupId.AsString
        let stored = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
        let command =
            stored.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>()
            |> Promise.FromTask
            |> Promise.Then (fun stored->
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
                if toBeAdded |> List.isEmpty
                then Then.Nothing
                else
                    stored.Tokens.AddRange toBeAdded
                    let options = UpdateOptions()
                    options.IsUpsert <- true    
                    collection.ReplaceOneAsync<StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, options)
                    :> System.Threading.Tasks.Task
                )
            |> Promise.Ignore
        command
    
    let StorePushNotificationSubscription (deviceGroupId : DeviceGroupId) (subscription : PushNotificationSubscription)=
        StorePushNotificationSubscriptions deviceGroupId [subscription]