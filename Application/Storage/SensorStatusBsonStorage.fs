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
    
    let FilterSensorsBy (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        let sensorId = sensorId.AsString
        let deviceGroupId = deviceGroupId.AsString
        let expr = ExpressionBuilder<StorableSensorStatus>.Filter(fun x -> x.DeviceGroupId = deviceGroupId && x.SensorId = sensorId)
        expr
    
    let FilterSensorsByEvent (event : SensorEvent) =
        FilterSensorsBy event.DeviceGroupId event.SensorId
        
    let Drop() =
        Database.DropCollection(SensorsCollectionName)
