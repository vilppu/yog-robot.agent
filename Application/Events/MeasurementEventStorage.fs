namespace YogRobot

[<AutoOpen>]
module MeasurementEventStorage = 
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open Microsoft.FSharp.Linq.RuntimeHelpers
    open Microsoft.FSharp.Reflection
    open MongoDB.Bson
    open MongoDB.Driver
    open MongoDB.Bson.Serialization.Attributes
    open Utility

    [<CLIMutable>]
    type StorableMeasurementEvent = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          DeviceGroupId : string
          DeviceId : string
          SensorId : string
          MeasuredProperty : string
          MeasuredValue : obj
          BatteryVoltage : float
          SignalStrength : float
          Timestamp : DateTime }
    
    let private measurementCases = FSharpType.GetUnionCases typeof<Measurement.Measurement>
    
    let private measurementEvents = 
        Database.GetCollection<StorableMeasurementEvent> "MeasurementEvents"
        |> WithDescendingIndex "DeviceGroupId"
        |> WithDescendingIndex "DeviceId"
        |> WithDescendingIndex "Timestamp"
    
    let private toMeasurement name (value : obj) = 
        let toMeasurementUnionCase case =
            FSharpValue.MakeUnion(case, [| value |]) :?> YogRobot.Measurement.Measurement
        
        let value = 
            measurementCases
            |> Array.toList
            |> List.filter (fun case -> case.Name = name)
            |> List.map toMeasurementUnionCase
        
        match value with
        | [] -> None
        | head :: tail -> Some(head)
    
    let private toMeasurementEvent(event : StorableMeasurementEvent) : MeasurementEvent = 
        let measurementOption = toMeasurement event.MeasuredProperty event.MeasuredValue
        match measurementOption with
        | Some measurement -> 
            let sensor : Sensor =
                { DeviceGroupId = DeviceGroupId event.DeviceGroupId
                  DeviceId = DeviceId event.DeviceGroupId
                  SensorId = SensorId event.DeviceGroupId
                  BatteryVoltage = event.BatteryVoltage<V>
                  SignalStrength = event.SignalStrength }
            Some { Measurement = measurement
                   Sensor = sensor
                   Timestamp = Timestamp event.Timestamp }
        | None -> None
    
    let private toMeasurementEvents documents = 
        documents
        |> Seq.map toMeasurementEvent
        |> Seq.choose id
        |> Seq.toList
    
    let Drop() = Database.DropCollection(measurementEvents.CollectionNamespace.CollectionName)
    
    let StoreMeasurementEvent (deviceGroupId : DeviceGroupId) (event : MeasurementEvent) = 
        let eventToBeStored = 
            let measurement = StorableMeasurement event.Measurement
            { Id = ObjectId.Empty
              DeviceGroupId = deviceGroupId.AsString
              DeviceId = event.DeviceId.AsString
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              Timestamp = event.Timestamp.AsDateTime }
        measurementEvents.InsertOneAsync(eventToBeStored)
        
    
    let StoreMeasurementEvents deviceGroupId events =         
        let store event = 
            StoreMeasurementEvent deviceGroupId event
        
        events
        |> Seq.map store
        |> AfterAll
