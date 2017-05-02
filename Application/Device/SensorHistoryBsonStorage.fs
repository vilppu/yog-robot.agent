namespace YogRobot

[<AutoOpen>]
module SensorHistoryBsonStorage =
    open System
    open System.Collections.Generic
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver

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
          DeviceId : string          
          SensorId : string
          MeasuredProperty : string
          Entries : List<StorableSensorHistoryEntry> }

    let SensorHistoryCollectionName = "SensorHistory"

    let SensorHistoryCollection = 
        Database.GetCollection<StorableSensorHistory> SensorHistoryCollectionName
        |> WithDescendingIndex "DeviceGroupId"        
        |> WithDescendingIndex "DeviceId"
        |> WithDescendingIndex "MeasuredProperty"


    let Drop() = Database.DropCollection(SensorHistoryCollectionName)