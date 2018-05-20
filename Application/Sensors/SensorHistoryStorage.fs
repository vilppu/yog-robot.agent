namespace YogRobot

module internal SensorHistoryStorage =
    open System.Collections.Generic
    open MongoDB.Bson
    open MongoDB.Driver

    let private toEntry (entry : SensorHistoryBsonStorage.StorableSensorHistoryEntry) : SensorHistoryEntry =
        let measuredValue = entry.MeasuredValue
        { MeasuredValue = measuredValue
          Timestamp = entry.Timestamp.ToUniversalTime() }
          
    let private toHistoryEntries (stored : SensorHistoryBsonStorage.StorableSensorHistory) : SensorHistoryEntry list =
         stored.Entries
         |> List.ofSeq
         |> List.map toEntry

    let private toHistory(stored : SensorHistoryBsonStorage.StorableSensorHistory) : SensorHistory =
        if stored :> obj |> isNull then
            EmptySensorHistory
        else
            { SensorId = stored.SensorId
              MeasuredProperty= stored.MeasuredProperty
              Entries = stored |> toHistoryEntries }
    
    let private entryToStorable (entry : SensorHistoryEntry) : SensorHistoryBsonStorage.StorableSensorHistoryEntry =
        { Id = ObjectId.Empty
          MeasuredValue = entry.MeasuredValue
          Timestamp = entry.Timestamp }

    let private updatedHistoryEntries (sensorState :  SensorState) (history : SensorHistory) =
        let maxNumberOfEntries = 30
        let measurement = StorableTypes.StorableMeasurement sensorState.Measurement
        let newEntry  = 
            { MeasuredValue = measurement.Value
              Timestamp = sensorState.Timestamp }
        let newHistory = newEntry :: history.Entries
        newHistory
        |> List.truncate maxNumberOfEntries
        |> List.map entryToStorable
        
    let private upsertHistory (sensorState : SensorState) (history : SensorHistory) =
        let measurement = StorableTypes.StorableMeasurement sensorState.Measurement
        let updatedEntries = updatedHistoryEntries sensorState history
        let storable : SensorHistoryBsonStorage.StorableSensorHistory =
            { Id = ObjectId.Empty
              DeviceGroupId  = sensorState.DeviceGroupId.AsString
              SensorId  = sensorState.SensorId.AsString
              MeasuredProperty = measurement.Name
              Entries = new List<SensorHistoryBsonStorage.StorableSensorHistoryEntry>(updatedEntries) }            
          
        let filter = SensorHistoryBsonStorage.FilterHistoryBy sensorState.DeviceGroupId.AsString sensorState.SensorId.AsString
        
        SensorHistoryBsonStorage.SensorHistoryCollection.ReplaceOneAsync<SensorHistoryBsonStorage.StorableSensorHistory>(filter, storable, BsonStorage.Upsert)
        |> Async.AwaitTask
        |> Async.Ignore

    let ReadSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        async {
            let filter = SensorHistoryBsonStorage.FilterHistoryBy deviceGroupId.AsString sensorId.AsString
            let history = SensorHistoryBsonStorage.SensorHistoryCollection.Find<SensorHistoryBsonStorage.StorableSensorHistory>(filter)
            let! first = history.FirstOrDefaultAsync<SensorHistoryBsonStorage.StorableSensorHistory>() |> Async.AwaitTask
            return first |> toHistory
        }
         
    let UpdateSensorHistory (sensonHistory : SensorHistory) (sensorState : SensorState) =
        async {
            let measurement = StorableTypes.StorableMeasurement sensorState.Measurement            
            let changed =
                match sensonHistory.Entries with
                | head::tail ->
                    head.MeasuredValue <> measurement.Value
                | _ -> true

            match changed with
            | true -> do! upsertHistory sensorState sensonHistory
            | false -> ()
        }
