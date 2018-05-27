namespace YogRobot

module KeyBsonStorage = 
    open System
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver

    [<CLIMutable>]
    type StorableMasterKey = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          Key : string
          ValidThrough : DateTime
          Timestamp : DateTime }

    [<CLIMutable>]
    type StorableDeviceGroupKey = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          Key : string
          DeviceGroupId : string 
          ValidThrough : DateTime
          Timestamp : DateTime }

    [<CLIMutable>]
    type StorableSensorKey = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          Key : string
          DeviceGroupId : string 
          ValidThrough : DateTime
          Timestamp : DateTime }
    
    let private masterKeys = BsonStorage.Database.GetCollection<StorableMasterKey> "masterKeys" |> BsonStorage.WithDescendingIndex "ValidThrough"
    
    let private deviceGroupKeys = 
        BsonStorage.Database.GetCollection<StorableDeviceGroupKey> "deviceGroupKeys"
        |> BsonStorage.WithDescendingIndex "ValidThrough"
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
    
    let private sensorKeys = 
        BsonStorage.Database.GetCollection<StorableSensorKey> "sensorKeys"
        |> BsonStorage.WithDescendingIndex "ValidThrough"
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
    
    let StoreMasterKey(key : StorableMasterKey) =
        masterKeys.InsertOneAsync(key)
        |> Async.AwaitTask
    
    let StoreDeviceGroupKey(key : StorableDeviceGroupKey) =
        deviceGroupKeys.InsertOneAsync(key)
        |> Async.AwaitTask
    
    let StoreSensorKey(key : StorableSensorKey) =
        sensorKeys.InsertOneAsync(key)
        |> Async.AwaitTask
    
    let GetMasterKeys (token : string) (validationTime : DateTime) : Async<string list> =
        async {
            let! result =
                masterKeys.FindAsync<StorableMasterKey>(fun k -> k.ValidThrough >= validationTime && k.Key = token)
                |> Async.AwaitTask
                    
            return
                result.ToList()
                |> List.ofSeq
                |> List.map (fun k -> k.Key)
        }        
    
    let GetDeviceGroupKeys (deviceGroupId : string) (token : string) (validationTime : DateTime) : Async<string list> =
        async {
            let! result =
                deviceGroupKeys.FindAsync<StorableDeviceGroupKey>(fun k -> k.ValidThrough >= validationTime && k.Key = token && k.DeviceGroupId = deviceGroupId)
                |> Async.AwaitTask
                    
            return
                result.ToList()
                |> List.ofSeq
                |> List.map (fun k -> k.Key)
        }

    let GetSensorKeys (deviceGroupId : string) (token : string) (validationTime : DateTime) : Async<string list> =
        async {
            let! result =
                sensorKeys.FindAsync<StorableSensorKey>(fun k -> k.ValidThrough >= validationTime && k.Key = token && k.DeviceGroupId = deviceGroupId)
                |> Async.AwaitTask
                    
            return
                result.ToList()
                |> List.ofSeq
                |> List.map (fun k -> k.Key)
        }

    let Drop() =
        BsonStorage.Database.DropCollection(masterKeys.CollectionNamespace.CollectionName)
        BsonStorage.Database.DropCollection(deviceGroupKeys.CollectionNamespace.CollectionName)
        BsonStorage.Database.DropCollection(sensorKeys.CollectionNamespace.CollectionName)
