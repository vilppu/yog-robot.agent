namespace YogRobot

module PushNotificationTests = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    
    [<Fact(Skip = "because")>]
    let NotifyWhenContactChanges() = 
        use context = SetupContext()
        let example = Contact Contact.Open
        context |> WriteMeasurement(Fake.Measurement example)
        
        context |> GetExampleSensorStatuses |> ignore
        
        Assert.Equal(1, SentHttpRequests.Count)
    
   