namespace YogRobot

module SensorHistoryBsonStorage =
    open System
    open System.Collections.Generic
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
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

    let SensorHistoryCollection = 
        BsonStorage.Database.GetCollection<StorableSensorHistory> sensorHistoryCollectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"        
        |> BsonStorage.WithDescendingIndex "DeviceId"
        |> BsonStorage.WithDescendingIndex "MeasuredProperty"
    
    let FilterHistoryBy (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        let sensorId = sensorId.AsString
        let deviceGroupId = deviceGroupId.AsString
        let expr = Lambda.Create<StorableSensorHistory>(fun x -> x.DeviceGroupId = deviceGroupId && x.SensorId = sensorId)
        expr
    
    let Drop() = BsonStorage.Database.DropCollection(sensorHistoryCollectionName)