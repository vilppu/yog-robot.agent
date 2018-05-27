namespace YogRobot

module internal ConvertKey = 
    open System
    open MongoDB.Bson
    
    let ToStorableMasterKey (key : MasterKey) : KeyBsonStorage.StorableMasterKey =
        { Id = ObjectId.Empty
          Key = key.Token.AsString
          ValidThrough = key.ValidThrough
          Timestamp = DateTime.UtcNow }
    
    let ToStorableDeviceGroupKeykey (key : DeviceGroupKey) : KeyBsonStorage.StorableDeviceGroupKey = 
        { Id = ObjectId.Empty
          Key = key.Token.AsString
          DeviceGroupId = key.DeviceGroupId.AsString
          ValidThrough = key.ValidThrough
          Timestamp = DateTime.UtcNow }
    
    let ToStorableSensorKey (key : SensorKey) : KeyBsonStorage.StorableSensorKey = 
        { Id = ObjectId.Empty
          Key = key.Token.AsString
          DeviceGroupId = key.DeviceGroupId.AsString
          ValidThrough = key.ValidThrough
          Timestamp = DateTime.UtcNow }
 