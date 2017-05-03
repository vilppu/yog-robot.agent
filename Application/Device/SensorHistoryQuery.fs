namespace YogRobot

[<AutoOpen>]
module SensorHistoryQuery =
    open System
    open System.Collections.Generic
    
    open MongoDB.Bson
    open MongoDB.Driver
    open Utility

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
            { DeviceId = stored.DeviceId
              MeasuredProperty= stored.MeasuredProperty
              Entries = stored |> toHistoryEntries }

    let ReadSensorHistory (sensorId : SensorId) =
        let sensorId = sensorId.AsString
        let history = SensorHistoryCollection.Find<StorableSensorHistory>(fun x ->
            x.SensorId = sensorId)

        history.FirstOrDefaultAsync<StorableSensorHistory>()
        |> Then.Map toHistory