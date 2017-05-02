namespace YogRobot

[<AutoOpen>]
module TestHelpers = 
    
    
    let WriteMeasurement (measurement, deviceId) (context : Context) = 
        PostMeasurement context.SensorKeyToken context.DeviceGroupId deviceId measurement
        |> Async.RunSynchronously
        |> ignore
    
    let GetExampleSensorStatusesResponse (context : Context) = 
        GetSensorStatusesResponse context.DeviceGroupToken |> Async.RunSynchronously
    
    let GetExampleSensorStatuses(context : Context) = 
        GetSensorStatuses context.DeviceGroupToken |> Async.RunSynchronously
    
    let GetExampleSensorHistoryResponse sensorId (context : Context) = 
        GetSensorHistoryResponse context.DeviceGroupToken sensorId
        |> Async.RunSynchronously
    
    let GetExampleSensorHistory sensorId (context : Context) = 
        GetSensorHistory context.DeviceGroupToken sensorId
        |> Async.RunSynchronously
