namespace YogRobot

module SensorStateBsonStorage =
    open System
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    
    [<CLIMutable>]
    [<BsonIgnoreExtraElements>]
    type StorableSensorState = 
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

    let private SensorsCollection = 
        BsonStorage.Database.GetCollection<StorableSensorState> SensorsCollectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
    
    let private GetSensorExpression (deviceGroupId : string) (sensorId : string) =
        let sensorId = sensorId
        let deviceGroupId = deviceGroupId
        let expr = Expressions.Lambda.Create<StorableSensorState>(fun x -> x.DeviceGroupId = deviceGroupId && x.SensorId = sensorId)
        expr
    
    let private GetSensorsExpression (deviceGroupId : string) =        
        let deviceGroupId = deviceGroupId
        let expr = Expressions.Lambda.Create<StorableSensorState>(fun x -> x.DeviceGroupId = deviceGroupId)
        expr

    let StoreSensorName (deviceGroupId : string) (sensorId : string) (sensorName : string) =
        
        let filter =
            GetSensorExpression deviceGroupId sensorId

        let update =
            Builders<StorableSensorState>.Update.Set((fun s -> s.SensorName), sensorName)

        async {
            do! SensorsCollection.UpdateOneAsync<StorableSensorState>(filter, update)
                |> Async.AwaitTask
                |> Async.Ignore
        }    

    let StoreSensorState (sensorState : StorableSensorState) =
    
        let filter = GetSensorExpression sensorState.DeviceGroupId sensorState.SensorId
        
        let update =
            Builders<StorableSensorState>.Update             
             .Set((fun s -> s.DeviceGroupId), sensorState.DeviceGroupId)
             .Set((fun s -> s.DeviceId), sensorState.DeviceId)
             .Set((fun s -> s.SensorId), sensorState.SensorId)
             .Set((fun s -> s.SensorName), sensorState.SensorName)
             .Set((fun s -> s.MeasuredProperty), sensorState.MeasuredProperty)
             .Set((fun s -> s.MeasuredValue), sensorState.MeasuredValue)
             .Set((fun s -> s.BatteryVoltage), sensorState.BatteryVoltage)
             .Set((fun s -> s.SignalStrength), sensorState.SignalStrength)
             .Set((fun s -> s.LastUpdated), sensorState.LastUpdated)
             .Set((fun s -> s.LastActive), sensorState.LastActive)
        
        SensorsCollection.UpdateOneAsync<StorableSensorState>(filter, update, BsonStorage.Upsert)
        :> System.Threading.Tasks.Task
        |> Async.AwaitTask

    let GetSensorState deviceGroupId sensorId : Async<StorableSensorState> =
        async {
            let filter = GetSensorExpression deviceGroupId sensorId

            let! sensorState =
                SensorsCollection.FindSync<StorableSensorState>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask

            return sensorState
        }

    let GetSensorStates deviceGroupId : Async<StorableSensorState list> =
        async {
            let filter = GetSensorsExpression deviceGroupId

            let! sensorStates =
                SensorsCollection.FindSync<StorableSensorState>(filter).ToListAsync()
                |> Async.AwaitTask

            return sensorStates |> List.ofSeq
        }

    let DefaultState =
        { Id = ObjectId.Empty
          DeviceGroupId = ""
          DeviceId = ""
          SensorId = ""
          SensorName = ""
          MeasuredProperty = ""
          MeasuredValue = null
          BatteryVoltage = 0.0
          SignalStrength = 0.0
          LastUpdated = DateTime.UtcNow
          LastActive = DateTime.UtcNow }
        
    let Drop() =
        BsonStorage.Database.DropCollection(SensorsCollectionName)
