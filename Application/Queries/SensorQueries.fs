namespace YogRobot

module SensorQueries =
    
    let GetSensorStatuses deviceGroupId =
        SensorStatusesQuery.ReadSensorStatuses deviceGroupId

    let GetSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        SensorHistoryQuery.ReadSensorHistory deviceGroupId sensorId
