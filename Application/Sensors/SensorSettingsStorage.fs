namespace YogRobot

module SensorSettingsStorage =
    open MongoDB.Driver

    let ChangeSensorName deviceGroupId sensorId sensorName =
        async {
            let filter = SensorStatusBsonStorage.FilterSensorsBy deviceGroupId sensorId
            let update =
                Builders<SensorStatusBsonStorage.StorableSensorStatus>.Update
                 .Set((fun s -> s.SensorName), sensorName)
            do! SensorStatusBsonStorage.SensorsCollection.UpdateOneAsync<SensorStatusBsonStorage.StorableSensorStatus>(filter, update) |> Async.AwaitTask |> Async.Ignore
        }
