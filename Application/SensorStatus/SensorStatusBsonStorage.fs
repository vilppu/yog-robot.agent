namespace YogRobot

[<AutoOpen>]
module SensorStatusesBsonStorage =
    open System
    open System.Collections.Generic
    open Microsoft.FSharp.Reflection
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    
    [<CLIMutable>]
    [<BsonIgnoreExtraElements>]
    type StorableSensorStatus = 
        { [<BsonIgnoreIfDefault>]
          mutable Id : ObjectId
          mutable DeviceGroupId : string
          mutable DeviceId : string
          mutable SensorId : string
          mutable SensorName : string
          mutable MeasuredProperty : string
          mutable MeasuredValue : obj
          mutable BatteryVoltage : float
          mutable SignalStrength : float
          mutable LastUpdated : DateTime
          mutable LastActive : DateTime }

    let SensorsCollectionName = "Sensors"

    let SensorsCollection = 
        Database.GetCollection<StorableSensorStatus> SensorsCollectionName
        |> WithDescendingIndex "DeviceGroupId"
        
    let Drop() =
        Database.DropCollection(SensorsCollectionName)
