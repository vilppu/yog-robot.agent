namespace YogRobot

[<AutoOpen>]
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

    let SensorHistoryCollectionName = "SensorHistory"

    let SensorHistoryCollection = 
        Database.GetCollection<StorableSensorHistory> SensorHistoryCollectionName
        |> WithDescendingIndex "DeviceGroupId"        
        |> WithDescendingIndex "DeviceId"
        |> WithDescendingIndex "MeasuredProperty"
    
    let FilterHistoryBy (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        let sensorId = sensorId.AsString
        let deviceGroupId = deviceGroupId.AsString
        let expr = Lambda.Create<StorableSensorHistory>(fun x -> x.DeviceGroupId = deviceGroupId && x.SensorId = sensorId)
        expr
    
    let Drop() = Database.DropCollection(SensorHistoryCollectionName)