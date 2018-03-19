namespace YogRobot

module SensorNotifications =
    
    let SendPushNotifications httpSend (sensorState : SensorState) previousMeasurement =
        async {               
            let reason : PushNotifications.PushNotificationReason =
                { SensorState = sensorState
                  PreviousMeasurement = previousMeasurement}
            do!
                PushNotifications.SendPushNotifications httpSend reason                
        }