namespace YogRobot

module Command =
   
    type SubscribeToPushNotifications =
        { DeviceGroupId : DeviceGroupId
          Subscription : PushNotification.Subscription }

    type ChangeSensorState =
        { SensorState : SensorState }

    type ChangeSensorName = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          SensorName : string }

    type SaveMasterKey =
        { Key : MasterKey }
    
    type SaveDeviceGroupKey = 
        { Key : DeviceGroupKey }
    
    type SaveSensorKey =
        { Key : SensorKey }
    
    type Command =
        | SubscribeToPushNotifications of SubscribeToPushNotifications
        | ChangeSensorState of ChangeSensorState
        | ChangeSensorName of ChangeSensorName
        | SaveMasterKey of SaveMasterKey
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
            let! (lastUpdated, measuredValue) = SensorStateStorage.ReadPreviousState command.SensorState.DeviceGroupId command.SensorState.SensorId
            let event : Event.SensorStateChanged =
                { SensorState = command.SensorState
                  PreviousTimestamp = lastUpdated
                  PreviousMeasurement = measuredValue}
            return Event.SensorStateChanged event
        }

    let private sensorNameChangedEvent (command : ChangeSensorName) =
        let event : Event.SensorNameChanged =
            { SensorId = command.SensorId
              DeviceGroupId = command.DeviceGroupId
              SensorName = command.SensorName }
        Event.SensorNameChanged event

    let private saveMasterKeyEvent (command : SaveMasterKey) =
        let event : Event.SavedMasterKey =
            { Key = command.Key }
        Event.SavedMasterKey event

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

            | SaveMasterKey saveMasterKey ->
                return saveMasterKeyEvent saveMasterKey

            | SaveDeviceGroupKey saveDeviceGroupKey ->
                return saveDeviceGroupKeyEvent saveDeviceGroupKey

            | SaveSensorKey saveSensorKey ->
                return saveSensorKeyEvent saveSensorKey
       }
  
    let Execute httpSend (command : Command) =     
        async {
            let! event = createEventFromCommand command
            do! Event.Store event
            do! Event.Send httpSend event
        }
 