namespace YogRobot

module internal ConvertSensorHistory =
    open System.Collections.Generic
    open MongoDB.Bson

    let private toEntry (entry: SensorHistoryStorage.StorableSensorHistoryEntry) : SensorHistoryEntry =
        let measuredValue = entry.MeasuredValue

        { MeasuredValue = measuredValue
          Timestamp = entry.Timestamp.ToUniversalTime() }

    let private toHistoryEntries (stored: SensorHistoryStorage.StorableSensorHistory) : SensorHistoryEntry list =
        stored.Entries |> List.ofSeq |> List.map toEntry

    let private entryToStorable (entry: SensorHistoryEntry) : SensorHistoryStorage.StorableSensorHistoryEntry =
        { Id = ObjectId.Empty
          MeasuredValue = entry.MeasuredValue
          Timestamp = entry.Timestamp }

    let private updatedHistoryEntries (sensorState: SensorState) (history: SensorHistory) =
        let maxNumberOfEntries = 30
        let measurement = DataTransferObject.Measurement sensorState.Measurement

        let newEntry =
            { MeasuredValue = measurement.Value
              Timestamp = sensorState.LastUpdated }

        let newHistory = newEntry :: history.Entries

        newHistory
        |> List.truncate maxNumberOfEntries
        |> List.map entryToStorable

    let FromStorable (stored: SensorHistoryStorage.StorableSensorHistory) : SensorHistory =
        if stored :> obj |> isNull then
            EmptySensorHistory
        else
            { SensorId = stored.SensorId
              MeasuredProperty = stored.MeasuredProperty
              Entries = stored |> toHistoryEntries }

    let ToStorable (sensorState: SensorState) (history: SensorHistory) : SensorHistoryStorage.StorableSensorHistory =
        let measurement = DataTransferObject.Measurement sensorState.Measurement
        let updatedEntries = updatedHistoryEntries sensorState history

        let storable: SensorHistoryStorage.StorableSensorHistory =
            { Id = ObjectId.Empty
              DeviceGroupId = sensorState.DeviceGroupId.AsString
              SensorId = sensorState.SensorId.AsString
              MeasuredProperty = measurement.Name
              Entries = new List<SensorHistoryStorage.StorableSensorHistoryEntry>(updatedEntries) }

        storable
