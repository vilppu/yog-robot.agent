namespace YogRobot

module SensorHistoryTests = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    open FsUnit
    
    [<Fact>]
    let AuthenticationTokenIsChecked() = 
        use context = SetupWithExampleDeviceGroup()
        context.DeviceGroupToken <- InvalidToken

        let response = context |> GetExampleSensorHistoryResponse "device-1.rh"

        response.StatusCode |> should equal HttpStatusCode.Unauthorized
    
    [<Fact>]
    let SensorHistoryContainsMeasurements() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 78.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)

        let result = context |> GetExampleSensorHistory sensorId
        let entry = result.Entries.Head
        
        result.MeasuredProperty |> should equal "RelativeHumidity"
        entry.MeasuredValue |> should equal 78.0
    
    [<Fact>]
    let SensorHistoryContainsMeasurementsChronologically() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 78.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 78.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 80.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 79.0) deviceId)

        let result = context |> GetExampleSensorHistory sensorId
        let entry = result.Entries.Head      
        
        entry.MeasuredValue |> should equal 79.0
 
    [<Fact>]
    let SensorHistoryEntryContainsTimestamp() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 78.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)

        let result = context |> GetExampleSensorHistory sensorId
        let entry = result.Entries.Head

        (System.DateTime.UtcNow - entry.Timestamp).TotalMinutes |> should be (lessThan 1.0)

    [<Fact>]
    let DeviceSpecificMeasurementHistory() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 78.0
        let anotherExample = RelativeHumidity 80.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice anotherExample deviceId)

        let result = context |> GetExampleSensorHistory sensorId
        
        result.Entries.Length |> should equal 2
    
    [<Fact>]
    let MeasurementHistoryShouldBeLimitedToContainOnlyNearHistory() = 
        use context = SetupWithExampleDeviceGroup()
        let expectedValue = 80.0
        let expectedLimit = 30
        let latest = RelativeHumidity expectedValue
        let bigNumberOfEvents = expectedLimit * 2
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"
        for i in 1..bigNumberOfEvents do
            let measurement = RelativeHumidity(float (i))
            context |> WriteMeasurement(Fake.MeasurementFromDevice measurement deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice latest deviceId)

        let result = context |> GetExampleSensorHistory sensorId

        result.Entries.Length |> should equal expectedLimit
        result.Entries.Head.MeasuredValue |> should equal expectedValue
    
    [<Fact>]
    let StoreOnlyMeasurementChanges() = 
        use context = SetupWithExampleDeviceGroup()
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"

        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 77.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 78.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 77.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 77.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 81.0) deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice (RelativeHumidity 82.0) deviceId)
        
        let result = context |> GetExampleSensorHistory sensorId
        
        result.Entries.Length |> should equal 5

    
    [<Fact>]
    let StoreOnlyStateChanges() = 
        use context = SetupWithExampleDeviceGroup()
        let deviceId = "device-1"
        let sensorId = deviceId + ".contact"
        let measurement = Contact Contact.Open

        context |> WriteMeasurement(Fake.MeasurementFromDevice measurement deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice measurement deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice measurement deviceId)
        
        let result = context |> GetExampleSensorHistory sensorId
        
        result.Entries.Length |> should equal 1

    [<Fact>]
    let ShowHistoryPerDevice() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 77.0
        let anotherExample = RelativeHumidity 80.0
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"
        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)
        context |> WriteMeasurement(Fake.MeasurementFromDevice anotherExample "device-2")
        
        let result = context |> GetExampleSensorHistory sensorId
        
        result.Entries.Length |> should equal 1
    
    [<Fact>]
    let HistoryShouldNotContainEntriesFromOtherDeviceGroups() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 78.1
        let deviceId = "device-1"
        let sensorId = deviceId + ".rh"
        let savedDeviceGroupToken = context.DeviceGroupToken

        context.DeviceGroupToken <- context.AnotherDeviceGroupToken
        context |> WriteMeasurement(Fake.MeasurementFromDevice example deviceId)

        context.DeviceGroupToken <- savedDeviceGroupToken
        let result =  context |> GetExampleSensorHistory sensorId
        
        result.Entries.Length |> should equal 1
