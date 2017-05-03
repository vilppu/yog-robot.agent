namespace YogRobot

[<AutoOpen>]
module SensorHistoryCommand =
    open System
    open System.Collections.Generic
    
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    open Utility
    
    let private entryToStorable (entry : SensorHistoryEntry) =
        { Id = ObjectId.Empty
          MeasuredValue = entry.MeasuredValue
          Timestamp = entry.Timestamp }

    let private updatedHistoryEntries event (history : SensorHistory) =
        let maxNumberOfEntries = 30
        let measurement = StorableMeasurement event.Measurement
        let newEntry  = 
            { MeasuredValue = measurement.Value
              Timestamp = event.Timestamp.AsDateTime }
        let newHistory = newEntry :: history.Entries
        newHistory
        |> List.truncate maxNumberOfEntries
        |> List.map entryToStorable
        
    let private upsertHistory (event : MeasurementEvent) (history : SensorHistory) =
        let measurement = StorableMeasurement event.Measurement
        let updatedEntries = updatedHistoryEntries event history
        let storable : StorableSensorHistory =
            { Id = ObjectId.Empty
              DeviceGroupId  = event.Sensor.DeviceGroupId.AsString
              DeviceId  = event.Sensor.DeviceId.AsString
              SensorId  = event.Sensor.SensorId.AsString
              MeasuredProperty = measurement.Name
              Entries = new List<StorableSensorHistoryEntry>(updatedEntries) }            
    
        let sensorId = event.Sensor.SensorId.AsString
        let options = UpdateOptions()
        options.IsUpsert <- true
        
        SensorHistoryCollection.ReplaceOneAsync<StorableSensorHistory>(
            (fun x -> x.SensorId = sensorId), storable, options)
        :> Task
         
    let private upsertHistoryFromEvent event =
        let measurement = StorableMeasurement event.Measurement
        ReadSensorHistory event.Sensor.SensorId
        |> Then.Map (fun history ->
            let changed =
                match history.Entries with
                | head::tail ->
                    head.MeasuredValue <> measurement.Value
                | _ -> true

            match changed with
            | true -> upsertHistory event history
            | false -> Then.Nothing)
        
    let UpdateSensorHistory measurementEvents =
        let operations =
            measurementEvents
            |> List.map (fun event -> (upsertHistoryFromEvent event) |> Flatten)            
        Then.Combine operations      