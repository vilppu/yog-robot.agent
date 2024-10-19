namespace YogRobot

module Application =
    open System
    open DataTransferObject
    open System.Threading.Tasks

    let GenerateSecureToken () =
        let tokenBytes = Guid.NewGuid().ToByteArray()
        let tokenWithDashes = BitConverter.ToString tokenBytes
        tokenWithDashes.Replace("-", "")

    let StoredTokenSecret () = Security.StoredTokenSecret()

    let IsValidMasterKey token =
        task {
            let keys =
                match Security.StoredMasterKey() with
                | null -> []
                | key -> [ key ] |> List.filter (fun key -> key = token)

            return keys.Length > 0
        }

    let IsValidDeviceGroupKey deviceGroupId token validationTime =
        task {
            let! keys = KeyStorage.GetDeviceGroupKeys deviceGroupId token validationTime
            return keys.Length > 0
        }

    let IsValidSensorKey deviceGroupId token validationTime =
        task {
            let! keys = KeyStorage.GetSensorKeys deviceGroupId token validationTime
            return keys.Length > 0
        }

    let RegisterDeviceGroupKey deviceGroupId =
        let key: Security.DeviceGroupKey =
            { Token = Security.DeviceGroupKeyToken(GenerateSecureToken())
              DeviceGroupId = DeviceGroupId deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }

        async {
            do! KeyStorage.StoreDeviceGroupKey(key |> Security.ToStorableDeviceGroupKeykey)
            return key.Token.AsString
        }

    let RegisterSensorKey deviceGroupId =
        let key: Security.SensorKey =
            { Token = Security.SensorKeyToken(GenerateSecureToken())
              DeviceGroupId = DeviceGroupId deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }

        async {
            do! KeyStorage.StoreSensorKey(key |> Security.ToStorableSensorKey)
            return key.Token.AsString
        }

    let PostDeviceGroupKey sendFirebaseMulticastMessages deviceGroupId token : Task<string> =
        task {
            let key: Security.DeviceGroupKey =
                { Token = Security.DeviceGroupKeyToken token
                  DeviceGroupId = DeviceGroupId deviceGroupId
                  ValidThrough = System.DateTime.UtcNow.AddYears(10) }

            let command = Command.SaveDeviceGroupKey { Key = key }
            do! Command.Execute sendFirebaseMulticastMessages command
            return key.Token.AsString
        }

    let PostSensorKey sendFirebaseMulticastMessages deviceGroupId token : Task<string> =
        task {
            let key: Security.SensorKey =
                { Token = Security.SensorKeyToken token
                  DeviceGroupId = DeviceGroupId deviceGroupId
                  ValidThrough = System.DateTime.UtcNow.AddYears(10) }

            let command = Command.SaveSensorKey { Key = key }
            do! Command.Execute sendFirebaseMulticastMessages command
            return key.Token.AsString
        }

    let PostSensorName sendFirebaseMulticastMessages deviceGroupId sensorId sensorName : Task<unit> =
        task {
            let changeSensorName: Command.ChangeSensorName =
                { SensorId = SensorId sensorId
                  DeviceGroupId = DeviceGroupId deviceGroupId
                  SensorName = sensorName }

            let command = Command.ChangeSensorName changeSensorName
            do! Command.Execute sendFirebaseMulticastMessages command
        }

    let GetSensorState (deviceGroupId: string) : Task<DataTransferObject.SensorState list> =
        task {

            let! statuses = SensorStateStorage.GetSensorStates deviceGroupId
            let statuses = statuses |> ConvertSensortState.FromStorables
            let result = statuses |> SensorStateToDataTransferObject
            return result
        }

    let GetSensorHistory (deviceGroupId: string) (sensorId: string) : Task<DataTransferObject.SensorHistory> =
        task {
            let! history = SensorHistoryStorage.GetSensorHistory deviceGroupId sensorId

            let result =
                history
                |> ConvertSensorHistory.FromStorable
                |> SensorHistoryToDataTransferObject

            return result
        }

    let SubscribeToPushNotifications sendFirebaseMulticastMessages deviceGroupId (token: string) : Task<unit> =
        task {
            let subscription = Notification.Subscription token

            let subscribeToPushNotifications: Command.SubscribeToPushNotifications =
                { DeviceGroupId = (DeviceGroupId deviceGroupId)
                  Subscription = subscription }

            let command = Command.SubscribeToPushNotifications subscribeToPushNotifications
            do! Command.Execute sendFirebaseMulticastMessages command
        }

    let PostSensorData sendFirebaseMulticastMessages deviceGroupId (sensorData: SensorData) =
        task {
            let changeSensorStates =
                sensorData |> Command.ToChangeSensorStateCommands(DeviceGroupId deviceGroupId)

            for changeSensorState in changeSensorStates do
                let command = Command.ChangeSensorState changeSensorState
                do! Command.Execute sendFirebaseMulticastMessages command
        }
