namespace YogRobot

module PushNotificationSubscriptionBsonStorage =
    open System
    open System.Collections.Generic
    open System.Linq    
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver

    [<CLIMutable>]
    [<BsonIgnoreExtraElements>]
    type StorablePushNotificationSubscriptions = 
        { 
          [<BsonIgnoreIfDefault>]
          mutable Id : ObjectId
          mutable DeviceGroupId : string
          mutable Tokens : List<string> }

    let private PushNotificationSubscriptionCollectionName = "PushNotificationSubscriptions"

    let PushNotificationSubscriptionCollection = 
        BsonStorage.Database.GetCollection<StorablePushNotificationSubscriptions> PushNotificationSubscriptionCollectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
        
    let Drop() =
        BsonStorage.Database.DropCollection(PushNotificationSubscriptionCollectionName)
    
    let private removePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (tokens : string list)=
        let deviceGroupId = deviceGroupId.AsString
        let collection = PushNotificationSubscriptionCollection
        let stored = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
        async {
            let! stored = stored.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>() |> Async.AwaitTask
            stored.Tokens.RemoveAll (fun token -> tokens.Contains(token)) |> ignore
            return!
                collection.ReplaceOneAsync<StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, BsonStorage.Upsert)
                |> Async.AwaitTask
                |> Async.Ignore
        }
    
    let StorePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (tokens : string list) =
        async {
            let collection = PushNotificationSubscriptionCollection
            let deviceGroupId = deviceGroupId.AsString
            let stored = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
            let! stored = stored.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>() |> Async.AwaitTask
            let stored : StorablePushNotificationSubscriptions =
                if stored :> obj |> isNull then
                    { Id = ObjectId.Empty
                      DeviceGroupId = deviceGroupId
                      Tokens = new List<string>() }
                else stored
            let toBeAdded =
                tokens
                |> List.filter (fun token -> not(stored.Tokens.Contains(token)))
            if not(toBeAdded |> List.isEmpty) then
                do!
                    stored.Tokens.AddRange toBeAdded
                    collection.ReplaceOneAsync<StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, BsonStorage.Upsert)
                    |> Async.AwaitTask
                    |> Async.Ignore
        }
    
    let ReadPushNotificationSubscriptions (deviceGroupId : DeviceGroupId) : Async<List<String>> =         
        async {
            let collection = PushNotificationSubscriptionCollection
            let deviceGroupId = deviceGroupId.AsString
            let tokens = collection.Find<StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)

            let! subscriptionsForDeviceGroup =
                tokens.FirstOrDefaultAsync<StorablePushNotificationSubscriptions>()
                |> Async.AwaitTask

            let result =
                if subscriptionsForDeviceGroup :> obj |> isNull
                then new List<String>()
                else subscriptionsForDeviceGroup.Tokens

            return result
        }

    let RemoveRegistrations (deviceGroupId : DeviceGroupId) (tokens : string list) =
        async {
            if not(tokens.IsEmpty) then
                return! removePushNotificationSubscriptions deviceGroupId tokens
            else
                return ()
        }

    let AddRegistrations (deviceGroupId : DeviceGroupId) (tokens : string list) =
        async {
            if not(tokens.IsEmpty) then
                return! StorePushNotificationSubscriptions deviceGroupId tokens
            else
                return ()
        }
    