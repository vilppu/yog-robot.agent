namespace YogRobot

[<AutoOpen>]
module SensorHistoryQuery =
    open System    
    open MongoDB.Driver

    let private toEntry (entry : StorableSensorHistoryEntry) : SensorHistoryEntry =
        let measuredValue = entry.MeasuredValue
        { MeasuredValue = measuredValue
          Timestamp = entry.Timestamp.ToUniversalTime() }
          
    let private toHistoryEntries (stored : StorableSensorHistory) : SensorHistoryEntry list =
         stored.Entries
         |> List.ofSeq
         |> List.map toEntry

    let private toHistory(stored : StorableSensorHistory) : SensorHistory =
        if stored :> obj |> isNull then
            EmptySensorHistory
        else
            { SensorId = stored.SensorId
              MeasuredProperty= stored.MeasuredProperty
              Entries = stored |> toHistoryEntries }

    let ReadSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        async {
            let filter = FilterHistoryBy deviceGroupId sensorId
            let history = SensorHistoryCollection.Find<StorableSensorHistory>(filter)
            let! first = history.FirstOrDefaultAsync<StorableSensorHistory>() |> Async.AwaitTask
            return first |> toHistory
        }       