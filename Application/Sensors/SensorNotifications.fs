namespace YogRobot

module SensorNotifications =
    
    let SendPushNotifications httpSend (sensorState : SensorState) previousMeasurement =
        async {               
            let reason : PushNotifications.PushNotificationReason =
                { SensorState = sensorState
                  PreviousMeasurement = previousMeasurement}
            PushNotifications.SendPushNotifications httpSend reason
            // Do not wait for push notifications to be sent to notification provider.
            // This is to ensure that IoT hub does not need to wait for request to complete 
            // for too long.
            |> Async.Start
        }