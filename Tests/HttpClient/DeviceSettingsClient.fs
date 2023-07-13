﻿namespace YogRobot

[<AutoOpen>]
module DeviceSettingsClient =

    let PostSensorName token sensorId name =
        let apiUrl = sprintf "api/sensor/%s/name/%s" sensorId name
        Http.Post token apiUrl ""

    let ChangeSensorName token sensorId deviceSettings =
        async {
            do!
                PostSensorName token sensorId deviceSettings
                |> Http.ThrowExceptionOnFailure
        }
