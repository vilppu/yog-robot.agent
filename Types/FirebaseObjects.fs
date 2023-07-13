namespace YogRobot

module FirebaseObjects =

    [<CLIMutable>]
    type FirebaseDeviceNotificationContent =
        { deviceId: string
          sensorName: string
          measuredProperty: string
          measuredValue: obj
          timestamp: System.DateTime }

    [<CLIMutable>]
    type FirebasePushNotificationRequestData =
        { deviceNotification: FirebaseDeviceNotificationContent }

    [<CLIMutable>]
    type FirebasePushNotification =
        { data: FirebasePushNotificationRequestData
          registration_ids: string seq }

    [<CLIMutable>]
    type FirebaseResult =
        { mutable message_id: string
          mutable error: string
          mutable registration_id: string }

    [<CLIMutable>]
    type FirebaseResponse =
        { mutable multicast_id: int64
          mutable success: int64
          mutable failure: int64
          mutable canonical_ids: int64
          mutable results: List<FirebaseResult> }
