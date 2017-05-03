namespace YogRobot

[<AutoOpen>]
module DeviceSettingsCommand =
    open System
    open System.Collections.Generic
    
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    open Utility

    let UpdateDeviceName (sensorId : SensorId) (sensorName : string) =
        let sensorId = sensorId.AsString
        let filter = Builders<StorableSensorStatus>.Filter.Eq((fun s -> s.SensorId), sensorId)
        let update =
            Builders<StorableSensorStatus>.Update
             .Set((fun s -> s.SensorName), sensorName)
        let result = SensorsCollection.UpdateOneAsync<StorableSensorStatus>((fun s -> s.SensorId = sensorId), update)
        result :> Task
