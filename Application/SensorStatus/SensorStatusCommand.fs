namespace YogRobot

[<AutoOpen>]
module SensorStatusesCommand =
    open System
    open System.Collections.Generic
    open System.Threading.Tasks
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    open Utility

    // type UpdatedStatus = 
    //     { Status : SensorStatus
    //       HasChanged : bool }

    // type UpdatedSensorStatuses = 
    //     { Statuses : SensorStatus list
    //       UpdatedStatus : UpdatedStatus }
   
    // let private toStorable (sensorStatus : SensorStatus) : StorableSensorStatus =
    //     { Id = ObjectId.Empty
    //       DeviceGroupId = sensorStatus.DeviceGroupId
    //       DeviceId = sensorStatus.DeviceId
    //       SensorId = sensorStatus.SensorId
    //       SensorName = sensorStatus.SensorName
    //       MeasuredProperty = sensorStatus.MeasuredProperty
    //       MeasuredValue = sensorStatus.MeasuredValue
    //       BatteryVoltage = sensorStatus.BatteryVoltage
    //       SignalStrength = sensorStatus.SignalStrength
    //       LastUpdated = sensorStatus.LastUpdated
    //       LastActive = sensorStatus.LastActive }
        
    // let private updateStatus deviceGroupId (event : MeasurementEvent) (status : SensorStatus) =
    //     let measurement = StorableMeasurement event.Measurement
        
    //     { status with DeviceId = event.DeviceId.AsString
    //                  MeasuredProperty = measurement.Name
    //                  MeasuredValue = measurement.Value
    //                  LastUpdated = updated
    //                  LastActive = event.Timestamp.AsDateTime }

    // let private updateStatuses deviceGroupId event entries =
    //     let measurement = StorableMeasurement event.Measurement

    //     let isFromSameSensor (sensorStatus : SensorStatus) =
    //         (sensorStatus.DeviceId = event.DeviceId.AsString) && (sensorStatus.MeasuredProperty = measurement.Name)

    //     let isFromDifferentSensor (sensorStatus : SensorStatus) =
    //         not(isFromSameSensor sensorStatus)

    //     let statusesToBeLeftIntact = entries |> List.filter isFromDifferentSensor
    //     let statusToBeUpdaterOrEmpty = entries |> List.filter isFromSameSensor

    //     let statusToBeUpdated =
    //         match statusToBeUpdaterOrEmpty with
    //         | head::tail -> head
    //         | [] -> EmptySensorStatus
    
    //     let hasChanged = measurement.Value <> statusToBeUpdated.MeasuredValue


    //     let updated =
    //         if measurement.Value <> status.MeasuredValue
    //         then event.Timestamp.AsDateTime
    //         else status.LastUpdated 
        
    //     let updated =
    //         if hasChanged
    //         then event.Timestamp.AsDateTime
    //         else statusToBeUpdated.LastUpdated
        
    //     let updatedStatus = 
    //         { Status = statusToBeUpdated |> updateStatus deviceGroupId event
    //           HasChanged = hasChanged }

    //     { Statuses = statusesToBeLeftIntact |> List.append [updatedStatus.Status]
    //       UpdatedStatus = updatedStatus }

    // let rec private updateStatusesFromEvents events deviceGroupId entries =
    //     match events with
    //     | head::tail ->
    //         let updated = entries |> updateStatuses deviceGroupId head
    //         updateStatusesFromEvents tail deviceGroupId updated.Statuses
    //     | [] -> entries
    
    // let private updateSensorStatuses (deviceGroupId : DeviceGroupId) (statuses : SensorStatus list)  =
    //     let deviceGroupId = deviceGroupId.AsString
    //     let storable = statuses |> List.map toStorable
    //     let options = UpdateOptions()
    //     options.IsUpsert <- true    
    //     SensorsCollection.ReplaceOneAsync<StorableSensorStatus>((fun x -> x.DeviceGroupId = deviceGroupId), storable, options)
    //     :> Task

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
              LastUpdated = event.Timestamp.AsDateTime
              LastActive = event.Timestamp.AsDateTime }
        let result = SensorsCollection.InsertOneAsync(storable)
        result

    let private updateExisting (toBeUpdated : StorableSensorStatus) (event : MeasurementEvent) =
    
        let measurement = StorableMeasurement event.Measurement
        let batteryVoltage = (float)event.Sensor.BatteryVoltage
        let signalStrength = (float)event.Sensor.SignalStrength
        let hasChanged = measurement.Value <> toBeUpdated.MeasuredValue
        let sensorId = event.Sensor.SensorId.AsString
        let filter = Builders<StorableSensorStatus>.Filter.Eq((fun s -> s.SensorId), sensorId)
        let lastActive = event.Timestamp.AsDateTime
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
            |> Then (fun toBeUpdated ->
                if toBeUpdated :> obj |> isNull then
                    event |> insertNew
                else
                    event |> updateExisting toBeUpdated)
        result.Unwrap()

    let UpdateSensorStatuses event =
        let updatePromise = updateSensorStatus event
        let notifyPromise = 
            ReadSensorStatuses event.Sensor.DeviceGroupId
            |> Then (fun statuses ->
                SendPushNotificationsFor event.Sensor.DeviceGroupId statuses)
        Task.WhenAll [updatePromise; notifyPromise.Unwrap()]
