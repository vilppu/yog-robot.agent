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
    
    let sensorKeysCacheKey = "KeysStorage.SensorKeys"
    let options = MemoryCacheOptions()
    let cache = new MemoryCache(options)
    let private (<&>) left right = FilterDefinition<StorableDeviceGroupKey>.op_BitwiseAnd(left, right)
    
    let private filterCollection (filter : FilterDefinition<BsonDocument>) (collection : IMongoCollection<BsonDocument>) = 
        collection.Find<BsonDocument>(filter).ToEnumerable() |> Seq.toList
    
    let StoreMasterKey(key : MasterKey) = 
        let keyToBeStored : StorableMasterKey =
            { Id = ObjectId.Empty
              Key = key.Token.AsString
              ValidThrough = key.ValidThrough.AsDateTime
              Timestamp = Now().AsDateTime }
        masterKeys.InsertOneAsync(keyToBeStored)
    
    let StoreDeviceGroupKey(key : DeviceGroupKey) = 
        let keyToBeStored : StorableDeviceGroupKey =
            { Id = ObjectId.Empty
              Key = key.Token.AsString
              DeviceGroupId = key.DeviceGroupId.AsString
              ValidThrough = key.ValidThrough.AsDateTime
              Timestamp = Now().AsDateTime }
        botKeys.InsertOneAsync(keyToBeStored)
    
    let StoreSensorKey(key : SensorKey) = 
        let keyToBeStored : StorableSensorKey =
            { Id = ObjectId.Empty
              Key = key.Token.AsString
              DeviceGroupId = key.DeviceGroupId.AsString
              ValidThrough = key.ValidThrough.AsDateTime
              Timestamp = Now().AsDateTime }
        sensorKeys.InsertOneAsync(keyToBeStored)
    
    let IsValidMasterKeyToken (token : MasterKeyToken) (validationTime : Timestamp) =
        let token = token.AsString
        let validationTime = validationTime.AsDateTime
        
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
    
    let IsValidDeviceGroupKeyToken (deviceGroupId : DeviceGroupId) (token : DeviceGroupKeyToken) (validationTime : Timestamp) =
        let deviceGroupId = deviceGroupId.AsString
        let token = token.AsString
        let validationTime = validationTime.AsDateTime

        let keys = 
            botKeys.Find<StorableDeviceGroupKey>(fun k ->
                k.ValidThrough >= validationTime && k.Key = token && k.DeviceGroupId = deviceGroupId).ToList()
        
        keys.Count > 0
    
    let IsValidSensorKeyToken (deviceGroupId : DeviceGroupId) (token : SensorKeyToken) (validationTime : Timestamp) =
        let deviceGroupId = deviceGroupId.AsString
        let token = token.AsString
        let validationTime = validationTime.AsDateTime

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
