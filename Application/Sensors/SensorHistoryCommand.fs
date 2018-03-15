namespace YogRobot

module SensorHistoryCommand =
    open System.Collections.Generic
    open MongoDB.Bson
    open MongoDB.Driver
    
    let private entryToStorable (entry : SensorHistoryEntry) : SensorHistoryBsonStorage.StorableSensorHistoryEntry =
        { Id = ObjectId.Empty
          MeasuredValue = entry.MeasuredValue
          Timestamp = entry.Timestamp }

    let private updatedHistoryEntries event (history : SensorHistory) =
        let maxNumberOfEntries = 30
        let measurement = StorableTypes.StorableMeasurement event.Measurement
        let newEntry  = 
            { MeasuredValue = measurement.Value
              Timestamp = event.Timestamp }
        let newHistory = newEntry :: history.Entries
        newHistory
        |> List.truncate maxNumberOfEntries
        |> List.map entryToStorable
        
    let private upsertHistory (event : SensorStateChangedEvent) (history : SensorHistory) =
        let measurement = StorableTypes.StorableMeasurement event.Measurement
        let updatedEntries = updatedHistoryEntries event history
        let storable : SensorHistoryBsonStorage.StorableSensorHistory =
            { Id = ObjectId.Empty
              DeviceGroupId  = event.DeviceGroupId.AsString
              SensorId  = event.SensorId.AsString
              MeasuredProperty = measurement.Name
              Entries = new List<SensorHistoryBsonStorage.StorableSensorHistoryEntry>(updatedEntries) }            
          
        let filter = SensorHistoryBsonStorage.FilterHistoryBy event.DeviceGroupId event.SensorId
        let options = UpdateOptions()
        options.IsUpsert <- true
        
        SensorHistoryBsonStorage.SensorHistoryCollection.ReplaceOneAsync<SensorHistoryBsonStorage.StorableSensorHistory>(filter, storable, options)
        |> Async.AwaitTask
        |> Async.Ignore
         
    let UpdateSensorHistory event =
        async {
            let measurement = StorableTypes.StorableMeasurement event.Measurement
            let! history = SensorHistoryQuery.ReadSensorHistory event.DeviceGroupId event.SensorId
            let changed =
                match history.Entries with
                | head::tail ->
                    head.MeasuredValue <> measurement.Value
                | _ -> true

            match changed with
            | true -> do! upsertHistory event history
            | false -> ()
        }