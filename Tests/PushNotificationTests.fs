namespace YogRobot

module PushNotificationTests = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization

    [<CLIMutable>]
    type FirebaseDeviceNotificationContent = 
        { deviceId : string
          sensorName : string
          measuredProperty : string
          measuredValue : obj
          timestamp : DateTime }

    [<CLIMutable>]
    type FirebasePushNotificationRequestData = 
        { deviceNotification : FirebaseDeviceNotificationContent }

    [<CLIMutable>]
    type FirebasePushNotification = 
        { data : FirebasePushNotificationRequestData
          registration_ids : string seq }

    [<CLIMutable>]
    type FirebaseResult = 
        { mutable message_id : string
          mutable error : string
          mutable registration_id : string }

    [<CLIMutable>]
    type FirebaseResponse = 
        { mutable multicast_id : int64
          mutable success : int64
          mutable failure : int64
          mutable canonical_ids : int64
          mutable results : List<FirebaseResult> }

    [<Fact>]
    let NotifyWhenContactChanges() = 
        use context = SetupContext()
        let example = Contact Contact.Open

        context |> SetupToReceivePushNotifications

        context |> WriteMeasurement(Fake.Measurement example)
        
        Assert.Equal(1, SentHttpRequests.Count)
        Assert.Equal("https://fcm.googleapis.com/fcm/send", SentHttpRequests.[0].RequestUri.ToString())
   