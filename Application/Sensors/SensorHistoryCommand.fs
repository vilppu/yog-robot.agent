namespace YogRobot

[<AutoOpen>]
module SensorHistoryCommand =
    open System
    open System.Collections.Generic
    open System.Threading.Tasks
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    
    let private entryToStorable (entry : SensorHistoryEntry) =
        { Id = ObjectId.Empty
          MeasuredValue = entry.MeasuredValue
          Timestamp = entry.Timestamp }

    let private updatedHistoryEntries event (history : SensorHistory) =
        let maxNumberOfEntries = 30
        let measurement = StorableMeasurement event.Measurement
        let newEntry  = 
            { MeasuredValue = measurement.Value
              Timestamp = event.Timestamp }
        let newHistory = newEntry :: history.Entries
        newHistory
        |> List.truncate maxNumberOfEntries
        |> List.map entryToStorable
        
    let private upsertHistory (event : SensorEvent) (history : SensorHistory) =
        let measurement = StorableMeasurement event.Measurement
        let updatedEntries = updatedHistoryEntries event history
        let storable : StorableSensorHistory =
            { Id = ObjectId.Empty
              DeviceGroupId  = event.DeviceGroupId.AsString
              SensorId  = event.SensorId.AsString
              MeasuredProperty = measurement.Name
              Entries = new List<StorableSensorHistoryEntry>(updatedEntries) }            
          
        let filter = FilterHistoryBy event.DeviceGroupId event.SensorId
        let options = UpdateOptions()
        options.IsUpsert <- true
        
        SensorHistoryCollection.ReplaceOneAsync<StorableSensorHistory>(filter, storable, options)
        :> Task
         
    let UpdateSensorHistory event =
        let measurement = StorableMeasurement event.Measurement
        let promise =
            ReadSensorHistory event.DeviceGroupId event.SensorId
            |> Then.Map (fun history ->
                let changed =
                    match history.Entries with
                    | head::tail ->
                        head.MeasuredValue <> measurement.Value
                    | _ -> true

                match changed with
                | true -> upsertHistory event history |> Then.AsUnit
                | false -> Then.Nothing)
        promise.Unwrap()