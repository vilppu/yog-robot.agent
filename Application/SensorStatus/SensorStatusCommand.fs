namespace YogRobot

[<AutoOpen>]
module SensorStatusesCommand =
    open System
    open System.Collections.Generic
    
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    open Utility

    let private insertNew (event : MeasurementEvent) =
        let measurement = StorableMeasurement event.Measurement

        let storable : StorableSensorStatus =
            { Id = ObjectId.Empty
              DeviceGroupId = event.Sensor.DeviceGroupId.AsString
              DeviceId = event.Sensor.DeviceId.AsString
              SensorId = event.Sensor.SensorId.AsString
              SensorName = event.Sensor.DeviceId.AsString + measurement.Name
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              BatteryVoltage = (float)event.Sensor.BatteryVoltage
              SignalStrength = (float)event.Sensor.SignalStrength
              LastUpdated = event.Timestamp
              LastActive = event.Timestamp }
        let result = SensorsCollection.InsertOneAsync(storable)
        result

    let private updateExisting (toBeUpdated : StorableSensorStatus) (event : MeasurementEvent) =
    
        let measurement = StorableMeasurement event.Measurement
        let batteryVoltage = (float)event.Sensor.BatteryVoltage
        let signalStrength = (float)event.Sensor.SignalStrength
        let hasChanged = measurement.Value <> toBeUpdated.MeasuredValue
        let sensorId = event.Sensor.SensorId.AsString
        let filter = Builders<StorableSensorStatus>.Filter.Eq((fun s -> s.SensorId), sensorId)
        let lastActive = event.Timestamp
        let lastUpdated =
                    if hasChanged
                    then lastActive
                    else toBeUpdated.LastUpdated

        let filter = Builders<StorableSensorStatus>.Filter.Eq((fun s -> s.SensorId), sensorId)
        let update =
            Builders<StorableSensorStatus>.Update
             .Set((fun s -> s.MeasuredProperty), measurement.Name)
             .Set((fun s -> s.MeasuredValue), measurement.Value)
             .Set((fun s -> s.BatteryVoltage), batteryVoltage)
             .Set((fun s -> s.SignalStrength), signalStrength)
             .Set((fun s -> s.LastActive), lastActive)
             .Set((fun s -> s.LastUpdated), lastUpdated)
        let result = SensorsCollection.UpdateOneAsync<StorableSensorStatus>((fun s -> s.SensorId = sensorId), update)
        result :> Task

    let private updateSensorStatus (event : MeasurementEvent) : Task =
        let measurement = StorableMeasurement event.Measurement
        let sensorId = event.Sensor.SensorId.AsString
        let filter = Builders<StorableSensorStatus>.Filter.Eq((fun s -> s.SensorId), sensorId)
        
        let result = 
            SensorsCollection.FindSync<StorableSensorStatus>(filter).SingleOrDefaultAsync()
            |> Then.Map (fun toBeUpdated ->
                if toBeUpdated :> obj |> isNull then
                    event |> insertNew
                else
                    event |> updateExisting toBeUpdated)
        result.Unwrap()

    let UpdateSensorStatuses event =
        let updatePromise = updateSensorStatus event
        let notifyPromise = 
            ReadSensorStatuses event.Sensor.DeviceGroupId
            |> Then.Map (fun statuses ->
                let promise = SendPushNotifications event.Sensor.DeviceGroupId statuses
                promise)
        Then.Combine [updatePromise; notifyPromise.Unwrap()]
