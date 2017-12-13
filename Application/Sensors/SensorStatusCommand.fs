namespace YogRobot

[<AutoOpen>]
module SensorStatusesCommand =
    open System
    open System.Collections.Generic
    open System.Threading.Tasks    
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver

    let private insertNew (event : SensorEvent) =
        let measurement = StorableMeasurement event.Measurement

        let storable : StorableSensorStatus =
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
        let result = SensorsCollection.InsertOneAsync(storable)
        result

    let private updateExisting (sensorStatus : StorableSensorStatus) (event : SensorEvent) =
    
        let measurement = StorableMeasurement event.Measurement
        let voltage = (float)event.BatteryVoltage
        let signalStrength = (float)event.SignalStrength
        let hasChanged = measurement.Value <> sensorStatus.MeasuredValue
        let sensorId = event.SensorId.AsString
        let deviceGroupId = event.DeviceGroupId.AsString
        let lastActive = event.Timestamp
        let lastUpdated =
                    if hasChanged
                    then lastActive
                    else sensorStatus.LastUpdated
        let filter = event |> FilterSensorsByEvent
        
        let update =
            Builders<StorableSensorStatus>.Update
             .Set((fun s -> s.MeasuredProperty), measurement.Name)
             .Set((fun s -> s.MeasuredValue), measurement.Value)
             .Set((fun s -> s.BatteryVoltage), voltage)
             .Set((fun s -> s.SignalStrength), signalStrength)
             .Set((fun s -> s.LastActive), lastActive)
             .Set((fun s -> s.LastUpdated), lastUpdated)
        let result = SensorsCollection.UpdateOneAsync<StorableSensorStatus>(filter, update)
        result :> Task

    let HasChanges (event : SensorEvent) : Async<bool> =
        async {
            let measurement = StorableMeasurement event.Measurement
            let sensorId = event.SensorId.AsString
            let filter = event |> FilterSensorsByEvent
            let! sensorStatus =
                SensorsCollection.FindSync<StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            let result =
                (sensorStatus :> obj |> isNull) || (measurement.Value <> sensorStatus.MeasuredValue)
            return result
        }

    let UpdateSensorStatuses (httpSend) (event : SensorEvent) : Async<unit> =
        async {
            let sendPushNotifications = SendPushNotifications httpSend
            let measurement = StorableMeasurement event.Measurement
            let sensorId = event.SensorId.AsString
            let filter = event |> FilterSensorsByEvent
            let! sensorStatus =
                SensorsCollection.FindSync<StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask        

            do!
                if sensorStatus :> obj |> isNull then
                    event |> insertNew |> Async.AwaitTask
                else
                    event |> updateExisting sensorStatus |> Async.AwaitTask

            do!
                let reason =
                    { Event = event
                      Status = sensorStatus }
                sendPushNotifications reason
        }

