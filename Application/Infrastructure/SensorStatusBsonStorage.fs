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
    
    let FilterSensorsBy (deviceGroupId : string) (sensorId : string) =
        let sensorId = sensorId
        let deviceGroupId = deviceGroupId
        let expr = Expressions.Lambda.Create<StorableSensorStatus>(fun x -> x.DeviceGroupId = deviceGroupId && x.SensorId = sensorId)
        expr

    let StoreSensorName filter sensorName =
        
        let update =
            Builders<StorableSensorStatus>.Update.Set((fun s -> s.SensorName), sensorName)

        async {
            do! SensorsCollection.UpdateOneAsync<StorableSensorStatus>(filter, update)
                |> Async.AwaitTask
                |> Async.Ignore
        }
        
    let Drop() =
        BsonStorage.Database.DropCollection(SensorsCollectionName)
