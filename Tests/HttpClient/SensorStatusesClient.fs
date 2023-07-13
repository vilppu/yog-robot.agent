namespace YogRobot

[<AutoOpen>]
module SensorStateClient =
    open System.Text.Json
    open DataTransferObject

    let GetSensorStateResponse token =
        let apiUrl = "api/sensors"
        Http.Get token apiUrl

    let GetSensorState token =
        let response = GetSensorStateResponse token

        async {
            let! content = response |> Http.ContentOrFail

            let result = Json.Deserialize<List<DataTransferObject.SensorState>>(content)

            return result |> Seq.toList
        }

    let GetSensorHistoryResponse token sensorId =
        let apiUrl = sprintf "api/sensor/%s/history" sensorId
        Http.Get token apiUrl

    let GetSensorHistory token sensorId =
        let response = GetSensorHistoryResponse token sensorId

        async {
            let! content = response |> Http.ContentOrFail
            return Json.Deserialize<DataTransferObject.SensorHistory>(content)
        }
