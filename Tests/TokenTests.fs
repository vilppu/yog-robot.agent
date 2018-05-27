namespace YogRobot

module TokenTest = 
    open System.Net
    open Xunit
    
    [<Fact>]
    let MasterKeyIsRequiredToCreateMasterTokens() = 
        use context = SetupContext()
        let response = GetMasterTokenWithKey(InvalidToken) |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let DeviceGroupKeyIsRequiredToCreateDeviceGroupTokens() = 
        use context = SetupContext()
        let response = GetDeviceGroupTokenWithKey InvalidToken context.DeviceGroupId |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let DeviceGroupTokenCanBeUsedToAccessDeviceGroup() = 
        use context = SetupContext()
        let botToken = GetDeviceGroupToken context.DeviceGroupKeyToken context.DeviceGroupId |> Async.RunSynchronously
        let response = GetSensorStateResponse botToken |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    
    [<Fact>]
    let SensorKeyIsRequiredToCreateSensorTokens() = 
        use context = SetupContext()
        let response = GetSensorTokenWithKey InvalidToken context.DeviceGroupId |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
