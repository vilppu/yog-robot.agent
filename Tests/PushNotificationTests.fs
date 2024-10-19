namespace YogRobot

module PushNotificationTests =
    open Xunit

    let WaitForBackgroundProcessingToComplete () =
        System.Threading.Tasks.Task.Delay(100) |> Async.AwaitTask

    [<Fact>]
    let NotifyAboutContact () =
        async {
            use context = SetupContext()
            let opened = Measurement.Contact Measurement.Open
            let closed = Measurement.Contact Measurement.Closed

            context |> SetupToReceivePushNotifications

            context |> WriteMeasurementSynchronously(Fake.Measurement opened)

            context |> WriteMeasurementSynchronously(Fake.Measurement closed)

            do! WaitForBackgroundProcessingToComplete()

            Assert.Equal(2, SentFirebaseMessages.Count)
        }

    [<Fact>]
    let NotifyOnlyWhenContactChanges () =
        async {
            use context = SetupContext()
            let opened = Measurement.Contact Measurement.Open

            context |> SetupToReceivePushNotifications

            context |> WriteMeasurementSynchronously(Fake.Measurement opened)

            context |> WriteMeasurementSynchronously(Fake.Measurement opened)

            do! WaitForBackgroundProcessingToComplete()

            Assert.Equal(1, SentFirebaseMessages.Count)
        }

    [<Fact>]
    let NotifyAboutPresenceOfWater () =
        async {
            use context = SetupContext()
            let present = Measurement.PresenceOfWater Measurement.Present
            let notPresent = Measurement.PresenceOfWater Measurement.NotPresent

            context |> SetupToReceivePushNotifications

            context |> WriteMeasurementSynchronously(Fake.Measurement present)

            context |> WriteMeasurementSynchronously(Fake.Measurement notPresent)

            context |> WriteMeasurementSynchronously(Fake.Measurement present)

            do! WaitForBackgroundProcessingToComplete()

            Assert.Equal(3, SentFirebaseMessages.Count)
        }

    [<Fact>]
    let NotifyOnlyWhenPresenceOfWaterChanges () =
        async {
            use context = SetupContext()
            let present = Measurement.PresenceOfWater Measurement.Present

            context |> SetupToReceivePushNotifications

            context |> WriteMeasurementSynchronously(Fake.Measurement present)

            context |> WriteMeasurementSynchronously(Fake.Measurement present)

            do! WaitForBackgroundProcessingToComplete()

            Assert.Equal(1, SentFirebaseMessages.Count)
        }

    [<Fact>]
    let NotifyAboutMotion () =
        async {
            use context = SetupContext()
            let motion = Measurement.Measurement.Motion Measurement.Motion
            let noMotion = Measurement.Measurement.Motion Measurement.NoMotion

            context |> SetupToReceivePushNotifications

            context |> WriteMeasurementSynchronously(Fake.Measurement motion)

            context |> WriteMeasurementSynchronously(Fake.Measurement noMotion)

            context |> WriteMeasurementSynchronously(Fake.Measurement motion)

            do! WaitForBackgroundProcessingToComplete()

            Assert.Equal(2, SentFirebaseMessages.Count)
        }

    [<Fact>]
    let DoNotNotifyAboutNoMotion () =
        async {
            use context = SetupContext()
            let noMotion = Measurement.Measurement.Motion Measurement.NoMotion

            context |> SetupToReceivePushNotifications

            context |> WriteMeasurementSynchronously(Fake.Measurement noMotion)

            do! WaitForBackgroundProcessingToComplete()

            Assert.Equal(0, SentFirebaseMessages.Count)
        }

    [<Fact>]
    let NotifyOnlyWhenHasMotionChanges () =
        async {
            use context = SetupContext()
            let motion = Measurement.Measurement.Motion Measurement.Motion

            context |> SetupToReceivePushNotifications

            context |> WriteMeasurementSynchronously(Fake.Measurement motion)

            context |> WriteMeasurementSynchronously(Fake.Measurement motion)

            do! WaitForBackgroundProcessingToComplete()

            Assert.Equal(1, SentFirebaseMessages.Count)
        }

    [<Fact>]
    let SendSensorName () =
        async {
            use context = SetupContext()
            let expectedName = "ExampleSensorName"

            context |> SetupToReceivePushNotifications

            context
            |> WriteMeasurementSynchronously(Fake.Measurement(Measurement.Contact Measurement.Open))

            ChangeSensorName context.DeviceGroupToken "ExampleDevice.contact" expectedName
            |> Async.RunSynchronously

            context
            |> WriteMeasurementSynchronously(Fake.Measurement(Measurement.Contact Measurement.Closed))

            do! WaitForBackgroundProcessingToComplete()

            let notification =
                System.Text.Json.JsonSerializer.Deserialize<FirebaseObjects.FirebaseDeviceNotificationContent>(
                    SentFirebaseMessages[1].Data["deviceNotification"]
                )

            Assert.Equal(expectedName, notification.sensorName)
        }
