﻿namespace YogRobot

module KeyStorage =
    open System
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver

    [<CLIMutable>]
    type StorableMasterKey =
        { [<BsonIgnoreIfDefault>]
          Id: ObjectId
          Key: string
          ValidThrough: DateTime
          Timestamp: DateTime }

    [<CLIMutable>]
    type StorableDeviceGroupKey =
        { [<BsonIgnoreIfDefault>]
          Id: ObjectId
          Key: string
          DeviceGroupId: string
          ValidThrough: DateTime
          Timestamp: DateTime }

    [<CLIMutable>]
    type StorableSensorKey =
        { [<BsonIgnoreIfDefault>]
          Id: ObjectId
          Key: string
          DeviceGroupId: string
          ValidThrough: DateTime
          Timestamp: DateTime }

    let private deviceGroupKeys =
        BsonStorage.Database.GetCollection<StorableDeviceGroupKey> "DeviceGroupKeys"
        |> BsonStorage.WithDescendingIndex "ValidThrough"
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"

    let private sensorKeys =
        BsonStorage.Database.GetCollection<StorableSensorKey> "SensorKeys"
        |> BsonStorage.WithDescendingIndex "ValidThrough"
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"

    let StoreDeviceGroupKey (key: StorableDeviceGroupKey) =
        deviceGroupKeys.InsertOneAsync(key)
        |> Async.AwaitTask

    let StoreSensorKey (key: StorableSensorKey) =
        sensorKeys.InsertOneAsync(key) |> Async.AwaitTask

    let GetDeviceGroupKeys (deviceGroupId: string) (token: string) (validationTime: DateTime) : Async<string list> =
        async {
            let! result =
                deviceGroupKeys.FindAsync<StorableDeviceGroupKey> (fun k ->
                    k.ValidThrough >= validationTime
                    && k.Key = token
                    && k.DeviceGroupId = deviceGroupId)
                |> Async.AwaitTask

            return
                result.ToList()
                |> List.ofSeq
                |> List.map (fun k -> k.Key)
        }

    let GetSensorKeys (deviceGroupId: string) (token: string) (validationTime: DateTime) : Async<string list> =
        async {
            let! result =
                sensorKeys.FindAsync<StorableSensorKey> (fun k ->
                    k.ValidThrough >= validationTime
                    && k.Key = token
                    && k.DeviceGroupId = deviceGroupId)
                |> Async.AwaitTask

            return
                result.ToList()
                |> List.ofSeq
                |> List.map (fun k -> k.Key)
        }

    let Drop () =
        BsonStorage.Database.DropCollection(deviceGroupKeys.CollectionNamespace.CollectionName)
        BsonStorage.Database.DropCollection(sensorKeys.CollectionNamespace.CollectionName)
