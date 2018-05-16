namespace YogRobot

module SensorHistoryStorage =
    open System.Collections.Generic
    open MongoDB.Bson
    open MongoDB.Driver
    open System
    open MongoDB.Bson.Serialization.Attributes
    open YogRobot.Expressions

    [<CLIMutable>]
    [<BsonIgnoreExtraElements>]
    type StorableSensorHistoryEntry = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          MeasuredValue : obj
          Timestamp : DateTime }

    [<CLIMutable>]
    [<BsonIgnoreExtraElements>]
    type StorableSensorHistory = 
        { 
          [<BsonIgnoreIfDefault>]
          Id : ObjectId
          DeviceGroupId : string
          SensorId : string
          MeasuredProperty : string
          Entries : List<StorableSensorHistoryEntry> }

    let private sensorHistoryCollectionName = "SensorHistory"

    let private SensorHistoryCollection = 
        BsonStorage.Database.GetCollection<StorableSensorHistory> sensorHistoryCollectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"        
        |> BsonStorage.WithDescendingIndex "DeviceId"
        |> BsonStorage.WithDescendingIndex "MeasuredProperty"
    
    let private FilterHistoryBy (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        let sensorId = sensorId.AsString
        let deviceGroupId = deviceGroupId.AsString
        let expr = Lambda.Create<StorableSensorHistory>(fun x -> x.DeviceGroupId = deviceGroupId && x.SensorId = sensorId)
        expr

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

    let private readSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        async {
            let filter = FilterHistoryBy deviceGroupId sensorId
            let history = SensorHistoryCollection.Find<StorableSensorHistory>(filter)
            let! first = history.FirstOrDefaultAsync<StorableSensorHistory>() |> Async.AwaitTask
            return first |> toHistory
        }
    
    let private entryToStorable (entry : SensorHistoryEntry) : StorableSensorHistoryEntry =
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
        let storable : StorableSensorHistory =
            { Id = ObjectId.Empty
              DeviceGroupId  = sensorState.DeviceGroupId.AsString
              SensorId  = sensorState.SensorId.AsString
              MeasuredProperty = measurement.Name
              Entries = new List<StorableSensorHistoryEntry>(updatedEntries) }            
          
        let filter = FilterHistoryBy sensorState.DeviceGroupId sensorState.SensorId
        let options = UpdateOptions()
        options.IsUpsert <- true
        
        SensorHistoryCollection.ReplaceOneAsync<StorableSensorHistory>(filter, storable, options)
        |> Async.AwaitTask
        |> Async.Ignore
    
    let Drop() = BsonStorage.Database.DropCollection(sensorHistoryCollectionName)
         
    let UpdateSensorHistory (sensorState : SensorState) =
        async {
            let measurement = StorableTypes.StorableMeasurement sensorState.Measurement
            let! history = readSensorHistory sensorState.DeviceGroupId sensorState.SensorId
            let changed =
                match history.Entries with
                | head::tail ->
                    head.MeasuredValue <> measurement.Value
                | _ -> true

            match changed with
            | true -> do! upsertHistory sensorState history
            | false -> ()
        }
