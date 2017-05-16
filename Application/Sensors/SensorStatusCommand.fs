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

    let private updateExisting (toBeUpdated : StorableSensorStatus) (event : SensorEvent) =
    
        let measurement = StorableMeasurement event.Measurement
        let voltage = (float)event.BatteryVoltage
        let signalStrength = (float)event.SignalStrength
        let hasChanged = measurement.Value <> toBeUpdated.MeasuredValue
        let sensorId = event.SensorId.AsString
        let deviceGroupId = event.DeviceGroupId.AsString
        let lastActive = event.Timestamp
        let lastUpdated =
                    if hasChanged
                    then lastActive
                    else toBeUpdated.LastUpdated
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

    let HasChanges (event : SensorEvent) : Task<bool> =
        let measurement = StorableMeasurement event.Measurement
        let sensorId = event.SensorId.AsString
        let filter = event |> FilterSensorsByEvent
        
        let result = 
            SensorsCollection.FindSync<StorableSensorStatus>(filter).SingleOrDefaultAsync()
            |> Then.Map (fun toBeUpdated -> (toBeUpdated :> obj |> isNull) || (measurement.Value <> toBeUpdated.MeasuredValue))
        result

    let UpdateSensorStatuses (event : SensorEvent) : Task<unit> =
        let measurement = StorableMeasurement event.Measurement
        let sensorId = event.SensorId.AsString
        let filter = event |> FilterSensorsByEvent
        
        let result = 
            SensorsCollection.FindSync<StorableSensorStatus>(filter).SingleOrDefaultAsync()
            |> Then.Map (fun toBeUpdated ->
                let updatePromise =
                    if toBeUpdated :> obj |> isNull then
                        event |> insertNew
                    else
                        event |> updateExisting toBeUpdated
                    |> Then.AsUnit
                let notifyPromise =
                    event
                    |> SendPushNotifications toBeUpdated
                    |> Then.AsUnit
                Then.Combine [updatePromise; notifyPromise]
                )
        result |> Then.Unwrap |> Then.AsUnit

