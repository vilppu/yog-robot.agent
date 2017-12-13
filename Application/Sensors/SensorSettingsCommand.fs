namespace YogRobot

[<AutoOpen>]
module SensorNameCommand =
    open MongoDB.Driver

    let UpdateSensorName (deviceGroupId : DeviceGroupId) (sensorId : SensorId)  (sensorName : string) =
        async {
            let filter = FilterSensorsBy deviceGroupId sensorId
            let update =
                Builders<StorableSensorStatus>.Update
                 .Set((fun s -> s.SensorName), sensorName)
            do! SensorsCollection.UpdateOneAsync<StorableSensorStatus>(filter, update) |> Async.AwaitTask |> Async.Ignore
        }
