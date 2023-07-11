namespace YogRobot

[<AutoOpen>]
module TestContext =
    open System
    open System.Net.Http
    open System.Threading.Tasks

    [<assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)>]
    do ()

    let mutable serverTask: Task = null

    let InvalidToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzY290Y2guaW8iLCJleHAiOjEzMDA4MTkzODAsIm5hbWUiOiJDaHJpcyBTZXZpbGxlamEiLCJhZG1pbiI6dHJ1ZX0.03f329983b86f7d9a9f5fef85305880101d5e302afafa20154d094b229f75773"

    let TheMasterKey = "D4C144DA78C8FF923F3C56ADEB4F5113"
    let AnotherMasterKey = "F4C155DA78C8FF923F3C56ADEB4F5113"
    let TestDeviceGroupId = "TestDeviceGroup"
    let AnotherTestDeviceGroupId = "AnotherTestDeviceGroupId"

    let SetupEmptyEnvironmentUsing httpSend =
        Environment.SetEnvironmentVariable("YOG_BOT_BASE_URL", "http://127.0.0.1:18888/yog-robot/")
        Environment.SetEnvironmentVariable("YOG_MONGODB_DATABASE", "YogRobot_Test")
        Environment.SetEnvironmentVariable("YOG_MASTER_KEY", TheMasterKey)
        Environment.SetEnvironmentVariable("YOG_TOKEN_SECRET", "54F705300F6E110FB7316E89CB152E3B")
        Environment.SetEnvironmentVariable("YOG_FCM_KEY", "fake")
        KeyStorage.Drop()
        SensorEventStorage.Drop TestDeviceGroupId
        SensorEventStorage.Drop AnotherTestDeviceGroupId
        SensorStateStorage.Drop()
        SensorHistoryStorage.Drop()

        if serverTask |> isNull then
            serverTask <- CreateHttpServer httpSend

    let SentHttpRequests = System.Collections.Generic.List<HttpRequestMessage>()
    let SentHttpRequestContents = System.Collections.Generic.List<string>()

    let SetupEmptyEnvironment () =
        SentHttpRequests.Clear()
        SentHttpRequestContents.Clear()

        let httpSend (request: HttpRequestMessage) : Async<HttpResponseMessage> =
            async {
                let requestContent =
                    request.Content.ReadAsStringAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously

                SentHttpRequests.Add request
                SentHttpRequestContents.Add requestContent

                let response = new HttpResponseMessage()
                response.Content <- new StringContent("")
                return response
            }

        SetupEmptyEnvironmentUsing httpSend

    type Context() =
        do SetupEmptyEnvironment()
        member this.ExampleMasterKey = TheMasterKey
        member this.NotRegisteredMasterKey = AnotherMasterKey
        member val DeviceGroupId = Application.GenerateSecureToken() with get, set
        member val AnotherDeviceGroupId = Application.GenerateSecureToken() with get, set
        member val DeviceGroupKeyToken = Application.GenerateSecureToken() with get, set
        member val AnotherDeviceGroupKey = Application.GenerateSecureToken() with get, set
        member val SensorKeyToken = Application.GenerateSecureToken() with get, set
        member val AnotherSensorKey = Application.GenerateSecureToken() with get, set
        member val MasterToken = "MasterToken" with get, set
        member val DeviceGroupToken = "DeviceGroupToken" with get, set
        member val AnotherDeviceGroupToken = "AnotherDeviceGroupToken" with get, set
        member val SensorToken = "SensorToken" with get, set
        member val AnotherSensorToken = "AnotherSensorToken" with get, set

        interface IDisposable with
            member this.Dispose() = ()

    let SetupEmptyContext () =
        SetupEmptyEnvironment()
        new Context()

    let SetupContext () =
        SetupEmptyEnvironment()
        let context = new Context()
        context.DeviceGroupId <- TestDeviceGroupId
        context.AnotherDeviceGroupId <- AnotherTestDeviceGroupId

        context.DeviceGroupKeyToken <-
            Application.RegisterDeviceGroupKey context.DeviceGroupId
            |> Async.RunSynchronously

        context.AnotherDeviceGroupKey <-
            Application.RegisterDeviceGroupKey context.AnotherDeviceGroupId
            |> Async.RunSynchronously

        context.SensorKeyToken <-
            Application.RegisterSensorKey context.DeviceGroupId
            |> Async.RunSynchronously

        context.AnotherSensorKey <-
            Application.RegisterSensorKey context.AnotherDeviceGroupId
            |> Async.RunSynchronously

        context.MasterToken <- GenerateMasterAccessToken()
        context.DeviceGroupToken <- GenerateDeviceGroupAccessToken context.DeviceGroupId
        context.AnotherDeviceGroupToken <- GenerateDeviceGroupAccessToken context.AnotherDeviceGroupId
        context.SensorToken <- GenerateSensorAccessToken context.DeviceGroupId
        context.AnotherSensorToken <- GenerateSensorAccessToken context.AnotherDeviceGroupId
        context
