namespace YogRobot

module SensorSettingsCommand =
    open MongoDB.Driver

    let UpdateSensorName (deviceGroupId : DeviceGroupId) (sensorId : SensorId)  (sensorName : string) =
        async {
            let filter = SensorStatusBsonStorage.FilterSensorsBy deviceGroupId sensorId
            let update =
                Builders<SensorStatusBsonStorage.StorableSensorStatus>.Update
                 .Set((fun s -> s.SensorName), sensorName)
            do! SensorStatusBsonStorage.SensorsCollection.UpdateOneAsync<SensorStatusBsonStorage.StorableSensorStatus>(filter, update) |> Async.AwaitTask |> Async.Ignore
        }
