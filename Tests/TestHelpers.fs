namespace YogRobot

[<AutoOpen>]
module TestHelpers = 
    
    let WriteMeasurement (measurement, deviceId) (context : Context) = 
        PostMeasurement context.SensorKeyToken context.DeviceGroupId deviceId measurement

    let WriteMeasurementSynchronously (measurement, deviceId) (context : Context) = 
        WriteMeasurement (measurement, deviceId) context |> Async.RunSynchronously |> ignore
    
    let GetExampleSensorStatusesResponse (context : Context) = 
        GetSensorStatusesResponse context.DeviceGroupToken |> Async.RunSynchronously
    
    let GetExampleSensorStatuses(context : Context) = 
        GetSensorStatuses context.DeviceGroupToken |> Async.RunSynchronously
    
    let SetupToReceivePushNotifications(context : Context) = 
        SubscribeToPushNotifications context.DeviceGroupToken "12345"
        |> Async.RunSynchronously
        |> ignore
    
    let GetExampleSensorHistoryResponse sensorId (context : Context) = 
        GetSensorHistoryResponse context.DeviceGroupToken sensorId
        |> Async.RunSynchronously
    
    let GetExampleSensorHistory sensorId (context : Context) = 
        GetSensorHistory context.DeviceGroupToken sensorId
        |> Async.RunSynchronously
