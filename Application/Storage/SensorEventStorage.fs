namespace YogRobot

[<AutoOpen>]
module SensorEventStorage = 
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Threading.Tasks    
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Microsoft.FSharp.Linq.RuntimeHelpers
    open Microsoft.FSharp.Reflection
    open MongoDB.Bson
    open MongoDB.Driver
    open MongoDB.Bson.Serialization.Attributes

    [<CLIMutable>]
    type StorableSensorEvent = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          DeviceGroupId : string
          DeviceId : string
          SensorId : string
          MeasuredProperty : string
          MeasuredValue : obj
          Voltage : float
          SignalStrength : float
          Timestamp : DateTime }
    
    let private measurementCases = FSharpType.GetUnionCases typeof<Measurement.Measurement>
    
    let private sensorEvents = 
        Database.GetCollection<StorableSensorEvent> "SensorEvents"
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
    
    let private toSensorEvent(event : StorableSensorEvent) : SensorEvent option = 
        let measurementOption = toMeasurement event.MeasuredProperty event.MeasuredValue
        match measurementOption with
        | Some measurement ->
            let sensorEvent =
                { SensorId = SensorId event.SensorId
                  DeviceGroupId = DeviceGroupId event.DeviceGroupId
                  DeviceId = DeviceId event.DeviceId
                  Measurement = measurement
                  BatteryVoltage = event.Voltage * 1.0<V>
                  SignalStrength = event.SignalStrength
                  Timestamp = event.Timestamp }
            Some sensorEvent
        | None -> None
    
    let private toSensorEvents documents = 
        documents
        |> Seq.map toSensorEvent
        |> Seq.choose id
        |> Seq.toList
    
    let Drop() = Database.DropCollection(sensorEvents.CollectionNamespace.CollectionName)
    
    let StoreSensorEvent (event : SensorEvent) = 
        let eventToBeStored : StorableSensorEvent = 
            let measurement = StorableMeasurement event.Measurement
            { Id = ObjectId.Empty
              DeviceGroupId =  event.DeviceGroupId.AsString
              DeviceId = event.DeviceId.AsString
              SensorId = event.SensorId.AsString
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              Voltage = (float)event.BatteryVoltage
              SignalStrength = (float)event.SignalStrength
              Timestamp = event.Timestamp }
        sensorEvents.InsertOneAsync(eventToBeStored)
        
    
    let StoreSensorEvents events =
        let store event =
            let result =
                HasChanges event
                |> Then.Map (fun hasChanges -> 
                    if hasChanges then StoreSensorEvent event
                    else Then.Nothing)
            result.Unwrap()
        events
        |> Seq.map store
        |> Then.Combine
