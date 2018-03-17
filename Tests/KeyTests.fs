namespace YogRobot

module KeyTest = 
    open System.Net
    open Xunit
    
    [<Fact>]
    let MasterTokenIsRequiredToCreateMasterKey() = 
        use context = SetupContext()
        let response = PostCreateMasterKey InvalidToken |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let MasterKeyCanBeUsedToCreateMasterTokens() = 
        use context = SetupContext()
        let masterKey = CreateMasterKey context.MasterToken |> Async.RunSynchronously
        let response = GetMasterTokenWithKey masterKey |> Async.RunSynchronously
        let token = response.Content.ReadAsStringAsync()|> Async.AwaitTask  |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
        Assert.False(token |> System.String.IsNullOrEmpty)
    
    [<Fact>]
    let MasterTokenIsRequiredToCreateDeviceGroupKey() = 
        use context = SetupContext()
        let response = PostCreateDeviceGroupKey InvalidToken context.DeviceGroupId |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let DeviceGroupKeyCanBeUsedToCreateDeviceGroupTokens() = 
        use context = SetupContext()
        let botKey = CreateDeviceGroupKey context.MasterToken context.DeviceGroupId |> Async.RunSynchronously
        let response = GetDeviceGroupTokenWithKey botKey context.DeviceGroupId |> Async.RunSynchronously
        let token = response.Content.ReadAsStringAsync()|> Async.AwaitTask  |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
        Assert.False(token |> System.String.IsNullOrEmpty)
    
    [<Fact>]
    let MasterTokenIsRequiredToCreateSensorKey() = 
        use context = SetupContext()
        let response = PostCreateSensorKey InvalidToken context.DeviceGroupId |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let SensorKeyCanBeUsedToCreateSensorTokens() = 
        use context = SetupContext()
        let sensorKey = CreateSensorKey context.MasterToken context.DeviceGroupId |> Async.RunSynchronously
        let response = GetSensorTokenWithKey sensorKey context.DeviceGroupId |> Async.RunSynchronously
        let token = response.Content.ReadAsStringAsync()|> Async.AwaitTask  |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
        Assert.False(token |> System.String.IsNullOrEmpty)
