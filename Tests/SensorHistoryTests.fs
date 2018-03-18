namespace YogRobot

module SensorHistoryTests = 
    open System
    open System.Net
    open Xunit
    
    [<Fact>]
    let AuthenticationTokenIsChecked() = 
        use context = SetupContext()
        context.DeviceGroupToken <- InvalidToken

        let response = context |> GetExampleSensorHistoryResponse "device-1.rh"

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let SensorHistoryContainsMeasurements() = 
        use context = SetupContext()
        let example = Measurement.RelativeHumidity 78.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice example deviceId)

        let result = context |> GetExampleSensorHistory sensorId
        let entry = result.Entries.Head
        
        Assert.Equal("RelativeHumidity", result.MeasuredProperty)
        Assert.Equal(78.0, entry.MeasuredValue :?> float)
    
    [<Fact>]
    let SensorHistoryContainsMeasurementsChronologically() = 
        use context = SetupContext()
        let example = Measurement.RelativeHumidity 78.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice (Measurement.RelativeHumidity 78.0) deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice (Measurement.RelativeHumidity 80.0) deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice (Measurement.RelativeHumidity 79.0) deviceId)

        let result = context |> GetExampleSensorHistory sensorId
        let entry = result.Entries.Head      
        
        Assert.Equal(79.0, entry.MeasuredValue :?> float)
 
    [<Fact>]
    let SensorHistoryEntryContainsTimestamp() = 
        use context = SetupContext()
        let example = Measurement.RelativeHumidity 78.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice example deviceId)

        let result = context |> GetExampleSensorHistory sensorId
        let entry = result.Entries.Head

        Assert.True((System.DateTime.UtcNow - entry.Timestamp).TotalMinutes < 1.0)

    [<Fact>]
    let DeviceSpecificMeasurementHistory() = 
        use context = SetupContext()
        let example = Measurement.RelativeHumidity 78.0
        let anotherExample = Measurement.RelativeHumidity 80.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice example deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice anotherExample deviceId)

        let result = context |> GetExampleSensorHistory sensorId
        
        Assert.Equal(2, result.Entries.Length)
    
    [<Fact>]
    let MeasurementHistoryShouldBeLimitedToContainOnlyNearHistory() = 
        use context = SetupContext()
        let expectedValue = 80.0
        let expectedLimit = 30
        let latest = Measurement.RelativeHumidity expectedValue
        let bigNumberOfEvents = expectedLimit * 2
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"
        for i in 1..bigNumberOfEvents do
            let measurement = Measurement.RelativeHumidity(float (i))
            context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice measurement deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice latest deviceId)

        let result = context |> GetExampleSensorHistory sensorId

        Assert.Equal(expectedLimit, result.Entries.Length)
        Assert.Equal(expectedValue, result.Entries.Head.MeasuredValue :?> float)
    
    [<Fact>]
    let StoreOnlyMeasurementChanges() = 
        use context = SetupContext()
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice (Measurement.RelativeHumidity 77.0) deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice (Measurement.RelativeHumidity 78.0) deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice (Measurement.RelativeHumidity 77.0) deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice (Measurement.RelativeHumidity 77.0) deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice (Measurement.RelativeHumidity 81.0) deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice (Measurement.RelativeHumidity 82.0) deviceId)
        
        let result = context |> GetExampleSensorHistory sensorId
        
        Assert.Equal(5, result.Entries.Length)

    
    [<Fact>]
    let StoreOnlyStateChanges() = 
        use context = SetupContext()
        let deviceId = "device-1"
        let sensorId = deviceId + ".contact"
        let measurement = Measurement.Contact Measurement.Open

        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice measurement deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice measurement deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice measurement deviceId)
        
        let result = context |> GetExampleSensorHistory sensorId
        
        Assert.Equal(1, result.Entries.Length)

    [<Fact>]
    let ShowHistoryPerDevice() = 
        use context = SetupContext()
        let example = Measurement.RelativeHumidity 77.0
        let anotherExample = Measurement.RelativeHumidity 80.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice example deviceId)
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice anotherExample "device-2")
        
        let result = context |> GetExampleSensorHistory sensorId
        
        Assert.Equal(1, result.Entries.Length)
    
    [<Fact>]
    let HistoryShouldNotContainEntriesFromOtherDeviceGroups() = 
        use context = SetupContext()
        let example = Measurement.RelativeHumidity 78.1
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"
        let savedDeviceGroupToken = context.DeviceGroupToken

        context.DeviceGroupToken <- context.AnotherDeviceGroupToken
        context |> WriteMeasurementSynchronously(Fake.MeasurementFromDevice example deviceId)

        context.DeviceGroupToken <- savedDeviceGroupToken
        let result =  context |> GetExampleSensorHistory sensorId
        
        Assert.Equal(1, result.Entries.Length)
