namespace YogRobot

[<AutoOpen>]
module SensorNameCommand =
    open System
    open System.Collections.Generic
    open System.Threading.Tasks
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver

    let UpdateSensorName (deviceGroupId : DeviceGroupId) (sensorId : SensorId)  (sensorName : string) =
        let filter = FilterSensorsBy deviceGroupId sensorId
        let update =
            Builders<StorableSensorStatus>.Update
             .Set((fun s -> s.SensorName), sensorName)
        SensorsCollection.UpdateOneAsync<StorableSensorStatus>(filter, update)
        |> Then.AsUnit
