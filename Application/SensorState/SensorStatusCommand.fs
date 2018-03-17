namespace YogRobot

module SensorStatusCommand =
    open System.Threading.Tasks    
    open MongoDB.Bson
    open MongoDB.Driver

    let private insertNew (event : SensorStateChangedEvent) =
        let measurement = StorableTypes.StorableMeasurement event.Measurement

        let storable : SensorStatusBsonStorage.StorableSensorStatus =
            { Id = ObjectId.Empty
              DeviceGroupId = event.DeviceGroupId.AsString
              DeviceId = event.DeviceId.AsString
              SensorId = event.SensorId.AsString
              SensorName = event.DeviceId.AsString + "." + measurement.Name
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              BatteryVoltage = (float)event.BatteryVoltage
              SignalStrength = (float)event.SignalStrength
              LastUpdated = event.Timestamp
              LastActive = event.Timestamp }
        let result = SensorStatusBsonStorage.SensorsCollection.InsertOneAsync(storable)
        result

    let private updateExisting (sensorStatus : SensorStatusBsonStorage.StorableSensorStatus) (event : SensorStateChangedEvent) =
    
        let measurement = StorableTypes.StorableMeasurement event.Measurement
        let voltage = (float)event.BatteryVoltage
        let signalStrength = (float)event.SignalStrength
        let hasChanged = measurement.Value <> sensorStatus.MeasuredValue
        let lastActive = event.Timestamp
        let lastUpdated =
                    if hasChanged
                    then lastActive
                    else sensorStatus.LastUpdated
        let filter = event |> SensorStatusBsonStorage.FilterSensorsByEvent
        
        let update =
            Builders<SensorStatusBsonStorage.StorableSensorStatus>.Update
             .Set((fun s -> s.MeasuredProperty), measurement.Name)
             .Set((fun s -> s.MeasuredValue), measurement.Value)
             .Set((fun s -> s.BatteryVoltage), voltage)
             .Set((fun s -> s.SignalStrength), signalStrength)
             .Set((fun s -> s.LastActive), lastActive)
             .Set((fun s -> s.LastUpdated), lastUpdated)
        let result = SensorStatusBsonStorage.SensorsCollection.UpdateOneAsync<SensorStatusBsonStorage.StorableSensorStatus>(filter, update)
        result :> Task

    let HasChanges (event : SensorStateChangedEvent) : Async<bool> =
        async {
            let measurement = StorableTypes.StorableMeasurement event.Measurement
            let filter = event |> SensorStatusBsonStorage.FilterSensorsByEvent
            let! sensorStatus =
                SensorStatusBsonStorage.SensorsCollection.FindSync<SensorStatusBsonStorage.StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            let result =
                (sensorStatus :> obj |> isNull) || (measurement.Value <> sensorStatus.MeasuredValue)
            return result
        }

    let SaveSensorStatus (httpSend) (event : SensorStateChangedEvent) : Async<unit> =
        async {            
            let filter = event |> SensorStatusBsonStorage.FilterSensorsByEvent
            let! previousSensorStatus =
                SensorStatusBsonStorage.SensorsCollection.FindSync<SensorStatusBsonStorage.StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask        

            do!
                if previousSensorStatus :> obj |> isNull then
                    event |> insertNew |> Async.AwaitTask
                else
                    event |> updateExisting previousSensorStatus |> Async.AwaitTask

            do
                let reason : PushNotifications.PushNotificationReason =
                    { Event = event
                      SensorStatusBeforeEvent = previousSensorStatus }
                PushNotifications.SendPushNotifications httpSend reason
                // Do not wait for push notifications to be sent to notification provider.
                // This is to ensure that IoT hub does not need to wait for request to complete 
                // for too long.
                |> Async.Start
        }

