namespace YogRobot

module internal Security =     
    open System
    open MongoDB.Bson

    type MasterKeyToken = 
        | MasterKeyToken of string
        member this.AsString = 
            let (MasterKeyToken unwrapped) = this
            unwrapped
    
    type DeviceGroupKeyToken = 
        | DeviceGroupKeyToken of string
        member this.AsString = 
            let (DeviceGroupKeyToken unwrapped) = this
            unwrapped
    
    type SensorKeyToken = 
        | SensorKeyToken of string
        member this.AsString = 
            let (SensorKeyToken unwrapped) = this
            unwrapped
    
    type MasterKey = 
        { Token : MasterKeyToken
          ValidThrough : DateTime }
    
    type SensorKey = 
        { Token : SensorKeyToken
          DeviceGroupId : DeviceGroupId
          ValidThrough : DateTime }
    
    type DeviceGroupKey = 
        { Token : DeviceGroupKeyToken
          DeviceGroupId : DeviceGroupId
          ValidThrough : DateTime }
     
    let StoredMasterKey() = Environment.GetEnvironmentVariable("YOG_MASTER_KEY")

    let StoredTokenSecret() =
        let tokenSecret = Environment.GetEnvironmentVariable("YOG_TOKEN_SECRET")
        if tokenSecret |> isNull then
            eprintfn "Environment variable YOG_TOKEN_SECRET is not set."
        tokenSecret
    
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
