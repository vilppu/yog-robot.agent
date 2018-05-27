namespace YogRobot

module internal Command =
    open DataTransferObject
   
    type SubscribeToPushNotifications =
        { DeviceGroupId : DeviceGroupId
          Subscription : Notification.Subscription }

    type ChangeSensorState =
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement.Measurement
          BatteryVoltage : Measurement.Voltage
          SignalStrength : Measurement.Rssi
          Timestamp : System.DateTime }

    type ChangeSensorName = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          SensorName : string }
    
    type SaveDeviceGroupKey = 
        { Key : Security.DeviceGroupKey }
    
    type SaveSensorKey =
        { Key : Security.SensorKey }
    
    type Command =
        | SubscribeToPushNotifications of SubscribeToPushNotifications
        | ChangeSensorState of ChangeSensorState
        | ChangeSensorName of ChangeSensorName
        | SaveDeviceGroupKey of SaveDeviceGroupKey
        | SaveSensorKey of SaveSensorKey

    let private subscribedToPushNotificationsEvent (command : SubscribeToPushNotifications) =
        async {
            let event : Event.SubscribedToPushNotifications =
                { DeviceGroupId = command.DeviceGroupId
                  Subscription = command.Subscription }
            return Event.SubscribedToPushNotifications event
        }
    
    let private sensorStateChangedEvent (command : ChangeSensorState) =
        async {            
            let event : Event.SensorStateChanged =                
                { SensorId = command.SensorId
                  DeviceGroupId = command.DeviceGroupId
                  DeviceId = command.DeviceId
                  Measurement = command.Measurement
                  BatteryVoltage = command.BatteryVoltage
                  SignalStrength = command.SignalStrength
                  Timestamp = command.Timestamp }

            return Event.SensorStateChanged event
        }

    let private sensorNameChangedEvent (command : ChangeSensorName) =
        let event : Event.SensorNameChanged =
            { SensorId = command.SensorId
              DeviceGroupId = command.DeviceGroupId
              SensorName = command.SensorName }
        Event.SensorNameChanged event

    let private saveDeviceGroupKeyEvent (command : SaveDeviceGroupKey) =
        let event : Event.SavedDeviceGroupKey =
            { Key = command.Key }
        Event.SavedDeviceGroupKey event

    let private saveSensorKeyEvent (command : SaveSensorKey) =
        let event : Event.SavedSensorKey =
            { Key = command.Key }
        Event.SavedSensorKey event

    let private createEventFromCommand (command : Command) = 
        async {
            match command with
            | SubscribeToPushNotifications subscribeToPushNotifications ->
                return! subscribedToPushNotificationsEvent subscribeToPushNotifications 

            | ChangeSensorState changeSensorState ->
                return! sensorStateChangedEvent changeSensorState

            | ChangeSensorName changeSensorName ->
                return sensorNameChangedEvent changeSensorName

            | SaveDeviceGroupKey saveDeviceGroupKey ->
                return saveDeviceGroupKeyEvent saveDeviceGroupKey

            | SaveSensorKey saveSensorKey ->
                return saveSensorKeyEvent saveSensorKey
       }
    
    let private toChangeSensorStateCommand
        (deviceGroupId : DeviceGroupId)
        (sensorData : SensorData)
        (datum : SensorDatum)
        (timestamp : System.DateTime)
        : Option<ChangeSensorState> =
        
        let measurementOption = DataTransferObject.SensorDatumToMeasurement datum

        match measurementOption with
        | Some measurement ->
            let property = datum |> DataTransferObject.MeasuredPropertyName
            let deviceId = DeviceId sensorData.sensorId

            let command : ChangeSensorState =
                { SensorId = SensorId (deviceId.AsString + "." + property)
                  DeviceGroupId = deviceGroupId
                  DeviceId = deviceId
                  Measurement = measurement
                  BatteryVoltage = ToBatteryVoltage sensorData
                  SignalStrength = ToRssi sensorData
                  Timestamp = timestamp }

            Some command
        | None -> None

    let private toChangeSensorStateCommands (deviceGroupId : DeviceGroupId) (sensorData : SensorData) timestamp =
        sensorData.data
        |> Seq.toList
        |> List.map (fun datum -> toChangeSensorStateCommand deviceGroupId sensorData datum timestamp)
        |> List.choose (id)
    
    let ToChangeSensorStateCommands (deviceGroupId : DeviceGroupId) (sensorData : SensorData) : ChangeSensorState list = 
        let timestamp = System.DateTime.UtcNow
        let sensorData = ToGatewayEvent sensorData
        match sensorData with
        | GatewayEvent.SensorDataEvent sensorData ->
            toChangeSensorStateCommands deviceGroupId sensorData timestamp
        | _ -> []
  
    let Execute httpSend (command : Command) =     
        async {
            let! event = createEventFromCommand command
            do! Event.Store event
            do! Event.Send httpSend event
        }
 