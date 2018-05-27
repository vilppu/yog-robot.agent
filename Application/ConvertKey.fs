namespace YogRobot

module internal ConvertKey = 
    open System
    open MongoDB.Bson
    
    let ToStorableMasterKey (key : MasterKey) : KeyStorage.StorableMasterKey =
        { Id = ObjectId.Empty
          Key = key.Token.AsString
          ValidThrough = key.ValidThrough
          Timestamp = DateTime.UtcNow }
    
    let ToStorableDeviceGroupKeykey (key : DeviceGroupKey) : KeyStorage.StorableDeviceGroupKey = 
        { Id = ObjectId.Empty
          Key = key.Token.AsString
          DeviceGroupId = key.DeviceGroupId.AsString
          ValidThrough = key.ValidThrough
          Timestamp = DateTime.UtcNow }
    
    let ToStorableSensorKey (key : SensorKey) : KeyStorage.StorableSensorKey = 
        { Id = ObjectId.Empty
          Key = key.Token.AsString
          DeviceGroupId = key.DeviceGroupId.AsString
          ValidThrough = key.ValidThrough
          Timestamp = DateTime.UtcNow }
 