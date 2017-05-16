namespace YogRobot

module PushNotificationTests = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    open FsUnit
    
    [<Fact>]
    let NotifyWhenContactChanges() = 
        use context = SetupWithExampleDeviceGroup()
        let example = Contact Contact.Open
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        entry.MeasuredProperty |> should equal "Contact"
        entry.MeasuredValue |> should equal false
    
   