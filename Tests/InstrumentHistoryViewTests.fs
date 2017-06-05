namespace YogRobot

module SensorHistoryTests = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    
    [<Fact>]
    let AuthenticationTokenIsChecked() = 
        use context = SetupContext()
        context.DeviceGroupToken <- InvalidToken

        let response = context |> GetExampleSensorHistoryResponse "device-1" "RelativeHumidity"

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let SensorHistoryContainsMeasurements() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        let deviceId = "device-1"

        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)

        let result = context |> GetExampleSensorHistory deviceId "RelativeHumidity"
        let entry = result.Entries.Head
        
        Assert.Equal("RelativeHumidity", result.MeasuredProperty)
        Assert.Equal(78.0, entry.MeasuredValue)
    
    [<Fact>]
    let SensorHistoryContainsMeasurementsChronologically() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        let deviceId = "device-1"

        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 78.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 80.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 79.0) deviceId)

        let result = context |> GetExampleSensorHistory deviceId "RelativeHumidity"
        let entry = result.Entries.Head      
        
        Assert.Equal(79.0, entry.MeasuredValue)
 
    [<Fact>]
    let SensorHistoryEntryContainsTimestamp() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        let deviceId = "device-1"

        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)

        let result = context |> GetExampleSensorHistory deviceId "RelativeHumidity"
        let entry = result.Entries.Head

        (System.DateTime.UtcNow - entry.Timestamp).TotalMinutes |> should be (lessThan 1.0)

    [<Fact>]
    let DeviceGroupCanTellDeviceSpecificMeasurementHistory() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        let anotherExample = RelativeHumidity 80.0
        let deviceId = "device-1"

        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice anotherExample deviceId)

        let result = context |> GetExampleSensorHistory deviceId "RelativeHumidity"
        
        Assert.Equal(2, result.Entries.Length)
    
    [<Fact>]
    let MeasurementHistoryShouldBeLimitedToContainOnlyNearHistory() = 
        use context = SetupContext()
        let expectedValue = 80.0
        let expectedLimit = 30
        let latest = RelativeHumidity expectedValue
        let bigNumberOfEvents = expectedLimit * 2
        let deviceId = "device-1"
        for i in 1..bigNumberOfEvents do
            let measurement = RelativeHumidity(float (i))
            context |> WriteMeasurement(Fake.MeasurementFromDevice measurement deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice latest deviceId)

        let result = context |> GetExampleSensorHistory deviceId "RelativeHumidity"

        Assert.Equal(expectedLimit, result.Entries.Length)
        Assert.Equal(expectedValue, result.Entries.Head.MeasuredValue)
    
    [<Fact>]
    let StoreOnlyMeasurementChanges() = 
        use context = SetupContext()
        let deviceId = "device-1"

        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 77.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 78.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 77.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 77.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 81.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 82.0) deviceId)
        
        let result = context |> GetExampleSensorHistory deviceId "RelativeHumidity"
        
        Assert.Equal(5, result.Entries.Length)

    
    [<Fact>]
    let StoreOnlyStateChanges() = 
        use context = SetupContext()
        let deviceId = "device-1"
        let measurement = OpenClosed OpenClosed.Open

        context |> WriteMeasurement(Fake.MeasurementFromDevice measurement deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice measurement deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice measurement deviceId)
        
        let result = context |> GetExampleSensorHistory deviceId "OpenClosed"
        
        Assert.Equal(1, result.Entries.Length)

    [<Fact>]
    let ShowHistoryPerDevice() = 
        use context = SetupContext()
        let example = RelativeHumidity 77.0
        let anotherExample = RelativeHumidity 80.0
        let deviceId = "device-1"
        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice anotherExample "device-2")
        
        let result = context |> GetExampleSensorHistory deviceId "RelativeHumidity"
        
        Assert.Equal(1, result.Entries.Length)
    
    [<Fact>]
    let HistoryShouldNotContainEntriesFromOtherDeviceGroups() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.1
        let deviceId = "device-1"
        let savedDeviceGroupToken = context.DeviceGroupToken

        context.DeviceGroupToken <- context.AnotherDeviceGroupToken
        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)

        context.DeviceGroupToken <- savedDeviceGroupToken
        let result =  context |> GetExampleSensorHistory deviceId "RelativeHumidity"
        
        Assert.Equal(1, result.Entries.Length)
