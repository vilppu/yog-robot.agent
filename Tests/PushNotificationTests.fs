namespace YogRobot

module PushNotificationTests = 
    open System
    open Xunit

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
    let NotifyAboutContact() = 
        use context = SetupContext()
        let opened = Contact Contact.Open
        let closed = Contact Contact.Closed

        context |> SetupToReceivePushNotifications

        context |> WriteMeasurementSynchronously(Fake.Measurement opened)
        context |> WriteMeasurementSynchronously(Fake.Measurement closed)
        
        // Wait for background processing to complete.
        System.Threading.Thread.Sleep(100)

        Assert.Equal(2, SentHttpRequests.Count)
        Assert.Equal("https://fcm.googleapis.com/fcm/send", SentHttpRequests.[0].RequestUri.ToString())

    let WaitForBackgroundProcessingToComplete =
        System.Threading.Thread.Sleep(100)

    [<Fact>]
    let NotifyOnlyWhenContactChanges() = 
        use context = SetupContext()
        let opened = Contact Contact.Open

        context |> SetupToReceivePushNotifications

        context |> WriteMeasurementSynchronously(Fake.Measurement opened)        
        context |> WriteMeasurementSynchronously(Fake.Measurement opened)

        WaitForBackgroundProcessingToComplete

        Assert.Equal(1, SentHttpRequests.Count)

    [<Fact>]
    let NotifyAboutPresenceOfWater() = 
        use context = SetupContext()
        let present = PresenceOfWater PresenceOfWater.Present
        let notPresent = PresenceOfWater PresenceOfWater.NotPresent

        context |> SetupToReceivePushNotifications
        
        context |> WriteMeasurementSynchronously(Fake.Measurement present)
        context |> WriteMeasurementSynchronously(Fake.Measurement notPresent)
        
        WaitForBackgroundProcessingToComplete

        Assert.Equal(2, SentHttpRequests.Count)
        Assert.Equal("https://fcm.googleapis.com/fcm/send", SentHttpRequests.[0].RequestUri.ToString())

    [<Fact>]
    let NotifyOnlyWhenPresenceOfWaterChanges() = 
        use context = SetupContext()
        let present = PresenceOfWater PresenceOfWater.Present

        context |> SetupToReceivePushNotifications

        context |> WriteMeasurementSynchronously(Fake.Measurement present)
        context |> WriteMeasurementSynchronously(Fake.Measurement present)
        
        WaitForBackgroundProcessingToComplete

        Assert.Equal(1, SentHttpRequests.Count)
   