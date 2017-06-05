namespace YogRobot

module PushNotificationTests = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    open FsUnit
    
    [<Fact(Skip = "because")>]
    let NotifyWhenContactChanges() = 
        use context = SetupContext()
        let example = Contact Contact.Open
        context |> WriteMeasurement(Fake.Measurement example)
        
        context |> GetExampleSensorStatuses |> ignore
        
        SentHttpRequests.Count |> should equal 1
    
   