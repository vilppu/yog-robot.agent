namespace YogRobot

module SensorStateChangedEventHandler =
    open System.Collections.Generic
    open MongoDB.Bson
    open MongoDB.Driver
    open System.Threading.Tasks    

    let private insertNew (event : SensorStateChangedEvent) =
        let measurement = StorableTypes.StorableMeasurement event.Measurement

        let storable : SensorStatusBsonStorage.StorableSensorStatus =
            { Id = ObjectId.Empty
              DeviceGroupId = event.DeviceGroupId.AsString
              DeviceId = event.DeviceId.AsString
              SensorId = event.SensorId.AsString
              SensorName = event.DeviceId.AsString + "." + measurement.Name
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              BatteryVoltage = (float)event.BatteryVoltage
              SignalStrength = (float)event.SignalStrength
              LastUpdated = event.Timestamp
              LastActive = event.Timestamp }
        let result = SensorStatusBsonStorage.SensorsCollection.InsertOneAsync(storable)
        result

    let private updateExisting (sensorStatus : SensorStatusBsonStorage.StorableSensorStatus) (event : SensorStateChangedEvent) =
    
        let measurement = StorableTypes.StorableMeasurement event.Measurement
        let voltage = (float)event.BatteryVoltage
        let signalStrength = (float)event.SignalStrength
        let hasChanged = measurement.Value <> sensorStatus.MeasuredValue
        let lastActive = event.Timestamp
        let lastUpdated =
                    if hasChanged
                    then lastActive
                    else sensorStatus.LastUpdated
        let filter = event |> SensorStatusBsonStorage.FilterSensorsByEvent
        
        let update =
            Builders<SensorStatusBsonStorage.StorableSensorStatus>.Update
             .Set((fun s -> s.MeasuredProperty), measurement.Name)
             .Set((fun s -> s.MeasuredValue), measurement.Value)
             .Set((fun s -> s.BatteryVoltage), voltage)
             .Set((fun s -> s.SignalStrength), signalStrength)
             .Set((fun s -> s.LastActive), lastActive)
             .Set((fun s -> s.LastUpdated), lastUpdated)
        let result = SensorStatusBsonStorage.SensorsCollection.UpdateOneAsync<SensorStatusBsonStorage.StorableSensorStatus>(filter, update)
        result :> Task

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
            let filter = SensorHistoryBsonStorage.FilterHistoryBy deviceGroupId sensorId
            let history = SensorHistoryBsonStorage.SensorHistoryCollection.Find<SensorHistoryBsonStorage.StorableSensorHistory>(filter)
            let! first = history.FirstOrDefaultAsync<SensorHistoryBsonStorage.StorableSensorHistory>() |> Async.AwaitTask
            return first |> toHistory
        }
    
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
         
    let private updateSensorHistory event =
        async {
            let measurement = StorableTypes.StorableMeasurement event.Measurement
            let! history = ReadSensorHistory event.DeviceGroupId event.SensorId
            let changed =
                match history.Entries with
                | head::tail ->
                    head.MeasuredValue <> measurement.Value
                | _ -> true

            match changed with
            | true -> do! upsertHistory event history
            | false -> ()
        }
    
    let private UpdateSensorStatus (previousSensorStatus) (event : SensorStateChangedEvent) =
        async {             
            do!
                if previousSensorStatus :> obj |> isNull then
                    event |> insertNew |> Async.AwaitTask
                else
                    event |> updateExisting previousSensorStatus |> Async.AwaitTask
        }
    
    let private sendPushNotifications httpSend previousSensorStatus (event : SensorStateChangedEvent) =
        async {               
            let reason : PushNotifications.PushNotificationReason =
                { Event = event
                  SensorStatusBeforeEvent = previousSensorStatus }
            PushNotifications.SendPushNotifications httpSend reason
            // Do not wait for push notifications to be sent to notification provider.
            // This is to ensure that IoT hub does not need to wait for request to complete 
            // for too long.
            |> Async.Start
        }
    
    let OnSensorStateChanged httpSend (event : SensorStateChangedEvent) =
        async {
            let filter = event |> SensorStatusBsonStorage.FilterSensorsByEvent
            let! previousSensorStatus =
                SensorStatusBsonStorage.SensorsCollection.FindSync<SensorStatusBsonStorage.StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask     

            do! UpdateSensorStatus previousSensorStatus event
            do! updateSensorHistory event
            do! sendPushNotifications httpSend previousSensorStatus event
       }