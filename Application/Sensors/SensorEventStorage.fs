namespace YogRobot

module internal SensorEventStorage = 
    open MongoDB.Driver

    let private stateHasChanged (storableSensorEvent : SensorEventBsonStorage.StorableSensorEvent) : Async<bool> =
        async {
            let filter = SensorStateBsonStorage.FilterSensorsBy storableSensorEvent.DeviceGroupId storableSensorEvent.SensorId
            let! sensorState =
                SensorStateBsonStorage.SensorsCollection.FindSync<SensorStateBsonStorage.StorableSensorState>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            let result =
                (sensorState :> obj |> isNull) || (storableSensorEvent.MeasuredValue <> sensorState.MeasuredValue)
            return result
        }
    
    let StoreSensorEvent (storableSensorEvent : SensorEventBsonStorage.StorableSensorEvent) = 
        let collection = SensorEventBsonStorage.SensorEvents storableSensorEvent.DeviceGroupId
        async {
            let! hasChanges = stateHasChanged storableSensorEvent
            if hasChanges then
                do! collection.InsertOneAsync(storableSensorEvent) |> Async.AwaitTask
        }