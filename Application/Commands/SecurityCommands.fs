namespace YogRobot

module SecurityCommands =

    let SaveMasterKey key =
        KeyStorage.StoreMasterKey key
    
    let SaveDeviceGroupKey key = 
        KeyStorage.StoreDeviceGroupKey key
    
    let SaveSensorKey key =
        KeyStorage.StoreSensorKey key
