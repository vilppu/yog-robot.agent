namespace YogRobot

[<AutoOpen>]
module SensorHistoryQuery =
    open System
    open System.Collections.Generic
    
    open MongoDB.Bson
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
        let filter = FilterHistoryBy deviceGroupId sensorId
        let history = SensorHistoryCollection.Find<StorableSensorHistory>(filter)

        history.FirstOrDefaultAsync<StorableSensorHistory>()
        |> Then.Map toHistory