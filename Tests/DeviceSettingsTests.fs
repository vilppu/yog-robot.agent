namespace YogRobot

module DeviceSettingsTest = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    open FsUnit
    
    [<Fact>]
    let DeviceGroupTokenIsRequiredToSaveSensorName() = 
        use context = SetupContext()
        let deviceId = "ExampleDevice"      
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice deviceId)

        let response = PostSensorName InvalidToken deviceId "ExampleName" |> Async.RunSynchronously

        response.StatusCode |> should equal HttpStatusCode.Unauthorized
    
    
    [<Fact>]
    let SaveSensorName() = 
        use context = SetupContext()
        let expectedName = "ExampleSensorName"
        let deviceId = "ExampleDevice"
        let sensorId = "ExampleDevice.temperature"
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice deviceId)

        SaveSensorName context.DeviceGroupToken sensorId expectedName |> Async.RunSynchronously

        let result = context |> GetExampleSensorStatuses
        let entry = result.Head
        entry.SensorName |> should equal expectedName
