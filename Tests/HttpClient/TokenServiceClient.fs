namespace YogRobot

[<AutoOpen>]
module TokenServiceClient =
    open System.Text.Json
    open System.Net.Http

    let GetMasterTokenWithKey (key) =
        let apiUrl = "api/tokens/master"
        Agent.GetWithMasterKey key apiUrl

    let GetMasterToken masterKey =
        async {
            let! response =
                GetMasterTokenWithKey masterKey
                |> Http.ContentOrFail

            return Json.Deserialize<string>(response)
        }

    let GetDeviceGroupTokenWithKey botKey deviceGroupId : Async<HttpResponseMessage> =
        let apiUrl = "api/tokens/device-group"
        Agent.GetWithDeviceGroupKey botKey deviceGroupId apiUrl

    let GetDeviceGroupToken botKey deviceGroupId =
        async {
            let! response =
                GetDeviceGroupTokenWithKey botKey deviceGroupId
                |> Http.ContentOrFail

            return Json.Deserialize<string>(response)
        }

    let GetSensorTokenWithKey sensorKey deviceGroupId =
        let apiUrl = "api/tokens/sensor"
        Agent.GetWithSensorKey sensorKey deviceGroupId apiUrl

    let GetSensorToken sensorKey deviceGroupId =
        async {
            let! response =
                GetSensorTokenWithKey sensorKey deviceGroupId
                |> Http.ContentOrFail

            return Json.Deserialize<string>(response)
        }
