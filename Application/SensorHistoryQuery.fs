namespace YogRobot

module internal SensorHistoryQuery =
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

    let private ReadSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        async {
            let filter = SensorHistoryBsonStorage.FilterHistoryBy deviceGroupId.AsString sensorId.AsString
            let history = SensorHistoryBsonStorage.SensorHistoryCollection.Find<SensorHistoryBsonStorage.StorableSensorHistory>(filter)
            let! first = history.FirstOrDefaultAsync<SensorHistoryBsonStorage.StorableSensorHistory>() |> Async.AwaitTask
            return first |> toHistory
        }

    let GetSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        ReadSensorHistory deviceGroupId sensorId
