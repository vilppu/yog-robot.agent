namespace YogRobot

module DeviceSettingsTest = 
    open System.Net
    open Xunit
    
    [<Fact>]
    let DeviceGroupTokenIsRequiredToSaveSensorName() = 
        use context = SetupContext()
        let deviceId = "ExampleDevice"      
        context |> WriteMeasurementSynchronously(Fake.SomeMeasurementFromDevice deviceId)

        let response = PostSensorName InvalidToken deviceId "ExampleName" |> Async.RunSynchronously

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    
    [<Fact>]
    let SaveSensorName() = 
        use context = SetupContext()
        let expectedName = "ExampleSensorName"
        let deviceId = "ExampleDevice"
        let sensorId = "ExampleDevice.temperature"
        context |> WriteMeasurementSynchronously(Fake.SomeMeasurementFromDevice deviceId)

        SaveSensorName context.DeviceGroupToken sensorId expectedName |> Async.RunSynchronously

        let result = context |> GetExampleSensorStatuses
        let entry = result.Head
        Assert.Equal(expectedName, entry.SensorName)
