namespace YogRobot

module internal KeyStorage = 
    open System
    open MongoDB.Bson
    open MongoDB.Driver
    
    let StoreMasterKey(key : MasterKey) = 
        let keyToBeStored : KeyBsonStorage.StorableMasterKey =
            { Id = ObjectId.Empty
              Key = key.Token.AsString
              ValidThrough = key.ValidThrough
              Timestamp = DateTime.UtcNow }
        KeyBsonStorage.MasterKeys.InsertOneAsync(keyToBeStored)
        |> Async.AwaitTask
    
    let StoreDeviceGroupKey(key : DeviceGroupKey) = 
        let keyToBeStored : KeyBsonStorage.StorableDeviceGroupKey =
            { Id = ObjectId.Empty
              Key = key.Token.AsString
              DeviceGroupId = key.DeviceGroupId.AsString
              ValidThrough = key.ValidThrough
              Timestamp = DateTime.UtcNow }
        KeyBsonStorage.DeviceGroupKeys.InsertOneAsync(keyToBeStored)
        |> Async.AwaitTask
    
    let StoreSensorKey(key : SensorKey) = 
        let keyToBeStored : KeyBsonStorage.StorableSensorKey =
            { Id = ObjectId.Empty
              Key = key.Token.AsString
              DeviceGroupId = key.DeviceGroupId.AsString
              ValidThrough = key.ValidThrough
              Timestamp = DateTime.UtcNow }
        KeyBsonStorage.SensorKeys.InsertOneAsync(keyToBeStored)
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
                        KeyBsonStorage.MasterKeys.FindAsync<KeyBsonStorage.StorableMasterKey>(fun k -> k.ValidThrough >= validationTime && k.Key = token)
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
                        KeyBsonStorage.DeviceGroupKeys.FindAsync<KeyBsonStorage.StorableDeviceGroupKey>(fun k -> k.ValidThrough >= validationTime && k.Key = token && k.DeviceGroupId = deviceGroupId)
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
                        KeyBsonStorage.SensorKeys.FindAsync<KeyBsonStorage.StorableSensorKey>(fun k -> k.ValidThrough >= validationTime && k.Key = token && k.DeviceGroupId = deviceGroupId)
                        |> Async.AwaitTask
                    return result.ToList() |> List.ofSeq
                }
            
            let matchingKeys =
                keys
                |> List.filter (fun key -> key.DeviceGroupId = deviceGroupId)
                |> List.filter (fun key -> key.Key = token)
        
            return matchingKeys.Length > 0
        }
