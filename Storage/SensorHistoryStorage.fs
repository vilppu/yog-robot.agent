namespace YogRobot

module SensorHistoryStorage =
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
          Id: ObjectId
          MeasuredValue: obj
          Timestamp: DateTime }

    [<CLIMutable>]
    [<BsonIgnoreExtraElements>]
    type StorableSensorHistory =
        { [<BsonIgnoreIfDefault>]
          Id: ObjectId
          DeviceGroupId: string
          SensorId: string
          MeasuredProperty: string
          Entries: List<StorableSensorHistoryEntry> }

    let private sensorHistoryCollectionName = "SensorHistory"

    let private sensorHistoryCollection =
        BsonStorage.Database.GetCollection<StorableSensorHistory> sensorHistoryCollectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
        |> BsonStorage.WithDescendingIndex "DeviceId"
        |> BsonStorage.WithDescendingIndex "MeasuredProperty"

    let private filterHistoryBy (deviceGroupId: string) (sensorId: string) =
        let sensorId = sensorId
        let deviceGroupId = deviceGroupId

        let expr =
            Lambda.Create<StorableSensorHistory> (fun x ->
                x.DeviceGroupId = deviceGroupId
                && x.SensorId = sensorId)

        expr

    let GetSensorHistory (deviceGroupId: string) (sensorId: string) : Async<StorableSensorHistory> =
        async {
            let filter = filterHistoryBy deviceGroupId sensorId

            let! history =
                sensorHistoryCollection
                    .Find<StorableSensorHistory>(filter)
                    .FirstOrDefaultAsync<StorableSensorHistory>()
                |> Async.AwaitTask

            return history
        }

    let UpsertSensorHistory (history: StorableSensorHistory) =

        let filter = filterHistoryBy history.DeviceGroupId history.SensorId

        sensorHistoryCollection.ReplaceOneAsync<StorableSensorHistory>(filter, history, BsonStorage.Upsert)
        |> Async.AwaitTask
        |> Async.Ignore

    let Drop () =
        BsonStorage.Database.DropCollection(sensorHistoryCollectionName)
