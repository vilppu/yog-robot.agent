namespace YogRobot

module SensorStatusBsonStorage =
    open System
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

    let private SensorsCollectionName = "Sensors"

    let SensorsCollection = 
        BsonStorage.Database.GetCollection<StorableSensorStatus> SensorsCollectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
    
    let FilterSensorsBy (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        let sensorId = sensorId.AsString
        let deviceGroupId = deviceGroupId.AsString
        let expr = Expressions.Lambda.Create<StorableSensorStatus>(fun x -> x.DeviceGroupId = deviceGroupId && x.SensorId = sensorId)
        expr
    
    let FilterSensorsByEvent (event : Events.SensorStateChangedEvent) =
        FilterSensorsBy event.DeviceGroupId event.SensorId
        
    let Drop() =
        BsonStorage.Database.DropCollection(SensorsCollectionName)
