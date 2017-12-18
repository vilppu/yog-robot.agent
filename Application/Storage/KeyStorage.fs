namespace YogRobot

[<AutoOpen>]
module KeyStorage = 
    open System
    open Microsoft.Extensions.Caching.Memory
    open MongoDB.Bson
    open MongoDB.Driver
    open MongoDB.Bson.Serialization.Attributes

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
    
    let private masterKeys = Database.GetCollection<StorableMasterKey> "MasterKeys" |> WithDescendingIndex "ValidThrough"
    
    let private botKeys = 
        Database.GetCollection<StorableDeviceGroupKey> "DeviceGroupKeys"
        |> WithDescendingIndex "ValidThrough"
        |> WithDescendingIndex "DeviceGroupId"
    
    let private sensorKeys = 
        Database.GetCollection<StorableSensorKey> "SensorKeys"
        |> WithDescendingIndex "ValidThrough"
        |> WithDescendingIndex "DeviceGroupId"
    
    let private sensorKeysCacheKey = "KeysStorage.SensorKeys"
    let private options = MemoryCacheOptions()
    let private cache = new MemoryCache(options)
    
    let StoreMasterKey(key : MasterKey) = 
        let keyToBeStored : StorableMasterKey =
            { Id = ObjectId.Empty
              Key = key.Token.AsString
              ValidThrough = key.ValidThrough
              Timestamp = DateTime.UtcNow }
        masterKeys.InsertOneAsync(keyToBeStored)
        |> Async.AwaitTask
    
    let StoreDeviceGroupKey(key : DeviceGroupKey) = 
        let keyToBeStored : StorableDeviceGroupKey =
            { Id = ObjectId.Empty
              Key = key.Token.AsString
              DeviceGroupId = key.DeviceGroupId.AsString
              ValidThrough = key.ValidThrough
              Timestamp = DateTime.UtcNow }
        botKeys.InsertOneAsync(keyToBeStored)
        |> Async.AwaitTask
    
    let StoreSensorKey(key : SensorKey) = 
        let keyToBeStored : StorableSensorKey =
            { Id = ObjectId.Empty
              Key = key.Token.AsString
              DeviceGroupId = key.DeviceGroupId.AsString
              ValidThrough = key.ValidThrough
              Timestamp = DateTime.UtcNow }
        sensorKeys.InsertOneAsync(keyToBeStored)
        |> Async.AwaitTask
    
    let IsValidMasterKeyToken (token : MasterKeyToken) (validationTime : DateTime) =
        let token = token.AsString
        let validationTime = validationTime
        
        let configuredKeys = 
            match StoredMasterKey() with
            | null -> []
            | key -> [ key ] |> List.filter (fun key -> key = token)

        let keys = 
            masterKeys.Find<StorableMasterKey>(fun k ->
                k.ValidThrough >= validationTime && k.Key = token).ToList()
            |> List.ofSeq
            |> List.map (fun k -> k.Key)
            |> List.append configuredKeys
        
        keys.Length > 0
    
    let IsValidDeviceGroupKeyToken (deviceGroupId : DeviceGroupId) (token : DeviceGroupKeyToken) (validationTime : DateTime) =
        let deviceGroupId = deviceGroupId.AsString
        let token = token.AsString
        let validationTime = validationTime

        let keys = 
            botKeys.Find<StorableDeviceGroupKey>(fun k ->
                k.ValidThrough >= validationTime && k.Key = token && k.DeviceGroupId = deviceGroupId).ToList()
        
        keys.Count > 0
    
    let IsValidSensorKeyToken (deviceGroupId : DeviceGroupId) (token : SensorKeyToken) (validationTime : DateTime) =
        let deviceGroupId = deviceGroupId.AsString
        let token = token.AsString
        let validationTime = validationTime

        let valueFactory() = 
            let keys = 
                sensorKeys.Find<StorableSensorKey>(fun k ->
                    k.ValidThrough >= validationTime && k.Key = token && k.DeviceGroupId = deviceGroupId).ToList()                
            List.ofSeq keys
        
        let sensorKeys = new Lazy<StorableSensorKey list>(valueFactory)
        let (found, cached) = cache.TryGetValue(sensorKeysCacheKey)
        
        let keys =
            cache.GetOrCreate(sensorKeysCacheKey, (fun entry ->
                        entry.SlidingExpiration <- new Nullable<TimeSpan>(TimeSpan.FromSeconds(10.0))
                        sensorKeys
                        ))
        
        let matchingKeys = 
            keys.Value
            |> List.filter (fun key -> key.DeviceGroupId = deviceGroupId)
            |> List.filter (fun key -> key.Key = token)
        
        matchingKeys.Length > 0
    
    let Drop() = 
        cache.Remove sensorKeysCacheKey |> ignore
        Database.DropCollection(masterKeys.CollectionNamespace.CollectionName)
        Database.DropCollection(botKeys.CollectionNamespace.CollectionName)
        Database.DropCollection(sensorKeys.CollectionNamespace.CollectionName)
