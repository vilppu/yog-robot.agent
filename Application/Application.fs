namespace YogRobot

module Application =
    open System
    open DataTransferObject
    open System.Security.Cryptography

    let GenerateSecureToken() = 
        let randomNumberGenerator = RandomNumberGenerator.Create()
        let tokenBytes = Array.zeroCreate<byte> 16
        randomNumberGenerator.GetBytes tokenBytes
        let tokenWithDashes = BitConverter.ToString tokenBytes
        tokenWithDashes.Replace("-", "")

    let StoredTokenSecret() =
        Security.StoredTokenSecret()

    let IsValidMasterKeyToken token validationTime = 
        async {        
            let keys = 
                match StoredMasterKey() with
                | null -> []
                | key -> [ key ] |> List.filter (fun key -> key = token)
            return keys.Length > 0
        }

    let IsValidDeviceGroupKeyToken deviceGroupId token validationTime =
        async {
            let! keys = KeyBsonStorage.GetDeviceGroupKeys deviceGroupId token validationTime
            return keys.Length > 0
        }

    let IsValidSensorKeyToken deviceGroupId token validationTime = 
        async {
            let! keys = KeyBsonStorage.GetSensorKeys deviceGroupId token validationTime
            return keys.Length > 0
        }

    let RegisterMasterKey() = 
        let key : MasterKey = 
            { Token = MasterKeyToken(GenerateSecureToken())
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        async { 
            do! KeyBsonStorage.StoreMasterKey (key |> ConvertKey.ToStorableMasterKey)
            return key.Token.AsString
        }
    
    let RegisterDeviceGroupKey deviceGroupId = 
        let key : DeviceGroupKey = 
            { Token = DeviceGroupKeyToken(GenerateSecureToken())
              DeviceGroupId = DeviceGroupId deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        async { 
            do! KeyBsonStorage.StoreDeviceGroupKey (key |> ConvertKey.ToStorableDeviceGroupKeykey)
            return key.Token.AsString
        }
    
    let RegisterSensorKey deviceGroupId = 
        let key : SensorKey = 
            { Token = SensorKeyToken(GenerateSecureToken())
              DeviceGroupId = DeviceGroupId deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        async { 
            do! KeyBsonStorage.StoreSensorKey (key |> ConvertKey.ToStorableSensorKey)
            return key.Token.AsString
        }

    let PostMasterKey httpSend token : Async<string> = 
        async {
            let key : MasterKey = 
                { Token = MasterKeyToken token
                  ValidThrough = System.DateTime.UtcNow.AddYears(10) }
            let command = Command.SaveMasterKey { Key = key }
            do! Command.Execute httpSend command
            return key.Token.AsString
        }
    
    let PostDeviceGroupKey httpSend deviceGroupId token : Async<string> = 
        async {
            let key : DeviceGroupKey = 
                { Token = DeviceGroupKeyToken token
                  DeviceGroupId = DeviceGroupId deviceGroupId
                  ValidThrough = System.DateTime.UtcNow.AddYears(10) }
            let command = Command.SaveDeviceGroupKey { Key = key }
            do! Command.Execute httpSend command
            return key.Token.AsString
        }
    
    let PostSensorKey httpSend deviceGroupId token : Async<string> = 
        async {
            let key : SensorKey = 
                { Token = SensorKeyToken token
                  DeviceGroupId = DeviceGroupId deviceGroupId
                  ValidThrough = System.DateTime.UtcNow.AddYears(10) }
            let command = Command.SaveSensorKey { Key = key }
            do! Command.Execute httpSend command
            return key.Token.AsString
        }
    
    let PostSensorName httpSend deviceGroupId sensorId sensorName : Async<unit> = 
        async {    
            let changeSensorName : Command.ChangeSensorName =
                { SensorId = SensorId sensorId
                  DeviceGroupId = DeviceGroupId deviceGroupId
                  SensorName = sensorName}
            let command = Command.ChangeSensorName changeSensorName
            do! Command.Execute httpSend command
        }    
    
    let GetSensorState (deviceGroupId : string) : Async<DataTransferObject.SensorState list> = 
        async {
        
            let! statuses = SensorStateBsonStorage.GetSensorStates deviceGroupId
            let statuses = statuses |> ConvertSensortState.FromStorables
            let result = statuses |> SensorStateToDataTransferObject
            return result
        }

    let GetSensorHistory (deviceGroupId : string) (sensorId : string) : Async<DataTransferObject.SensorHistory> =
        async {
            let! history = SensorHistoryBsonStorage.GetSensorHistory deviceGroupId sensorId
            let result = history |> ConvertSensorHistory.FromStorable |> SensorHistoryToDataTransferObject
            return result
        }
    
    let SubscribeToPushNotifications httpSend deviceGroupId (token : string) : Async<unit> = 
        async {
            let subscription = Notification.Subscription token
            let subscribeToPushNotifications : Command.SubscribeToPushNotifications =
                { DeviceGroupId = (DeviceGroupId deviceGroupId)
                  Subscription = subscription }
            let command = Command.SubscribeToPushNotifications subscribeToPushNotifications
            do! Command.Execute httpSend command
        }

    let PostSensorData httpSend deviceGroupId (sensorData : SensorData) =
        async {
            let changeSensorStates = sensorData |> Command.ToChangeSensorStateCommands (DeviceGroupId deviceGroupId)
            for changeSensorState in changeSensorStates do
                let command = Command.ChangeSensorState changeSensorState
                do! Command.Execute httpSend command
        }
