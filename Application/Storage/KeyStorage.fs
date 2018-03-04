namespace YogRobot

[<AutoOpen>]
module KeyStorage = 
    open System
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
        async {
            let token = token.AsString
            let validationTime = validationTime
        
            let configuredKeys = 
                match StoredMasterKey() with
                | null -> []
                | key -> [ key ] |> List.filter (fun key -> key = token)

            let! keys =
                async {
                    let! result =
                        masterKeys.FindAsync<StorableMasterKey>(fun k -> k.ValidThrough >= validationTime && k.Key = token)
                        |> Async.AwaitTask
                    
                    return
                        result.ToList()
                        |> List.ofSeq
                        |> List.map (fun k -> k.Key)
                        |> List.append configuredKeys
                }        
            
            return keys.Length > 0
        }
    
    let IsValidDeviceGroupKeyToken (deviceGroupId : DeviceGroupId) (token : DeviceGroupKeyToken) (validationTime : DateTime) =
        async {
            let deviceGroupId = deviceGroupId.AsString
            let token = token.AsString
            let validationTime = validationTime

            let! keys =
                async {
                    let! result =
                        botKeys.FindAsync<StorableDeviceGroupKey>(fun k -> k.ValidThrough >= validationTime && k.Key = token && k.DeviceGroupId = deviceGroupId)
                        |> Async.AwaitTask
                    return result.ToList()
                 }
        
            return keys.Count > 0
        }

    let IsValidSensorKeyToken (deviceGroupId : DeviceGroupId) (token : SensorKeyToken) (validationTime : DateTime) =
        async {
            let deviceGroupId = deviceGroupId.AsString
            let token = token.AsString
            let validationTime = validationTime
        
            let! keys =
                async {
                    let! result =
                        sensorKeys.FindAsync<StorableSensorKey>(fun k -> k.ValidThrough >= validationTime && k.Key = token && k.DeviceGroupId = deviceGroupId)
                        |> Async.AwaitTask
                    return result.ToList() |> List.ofSeq
                }
            
            let matchingKeys =
                keys
                |> List.filter (fun key -> key.DeviceGroupId = deviceGroupId)
                |> List.filter (fun key -> key.Key = token)
        
            return matchingKeys.Length > 0
        }
    
    let Drop() =
        Database.DropCollection(masterKeys.CollectionNamespace.CollectionName)
        Database.DropCollection(botKeys.CollectionNamespace.CollectionName)
        Database.DropCollection(sensorKeys.CollectionNamespace.CollectionName)
