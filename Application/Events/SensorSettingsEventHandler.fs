namespace YogRobot

module SensorSettingsEventHandler =
    open MongoDB.Driver

    let OnSensorNameChanged (event : Events.SensorNameChangedEvent) =
        async {
            let filter = SensorStatusBsonStorage.FilterSensorsBy event.DeviceGroupId event.SensorId
            let update =
                Builders<SensorStatusBsonStorage.StorableSensorStatus>.Update
                 .Set((fun s -> s.SensorName), event.SensorName)
            do! SensorStatusBsonStorage.SensorsCollection.UpdateOneAsync<SensorStatusBsonStorage.StorableSensorStatus>(filter, update) |> Async.AwaitTask |> Async.Ignore
        }
