namespace YogRobot

module GetChanges = ()
//     open System
//     open System.Collections.Generic
//     open System.Threading.Tasks
//     open MongoDB.Bson
//     open MongoDB.Bson.Serialization.Attributes
//     open MongoDB.Driver
//     open Utility

//     type UpdatedSensorStatuses = 
//         { DeviceGroupId : DeviceGroupId
//           Entries : SensorStatus list
//           UpdatedEntries : SensorStatus list
//           MeasuredPropertyChanged : bool }

//     let private measuredPropertyChanged (entry : SensorStatus) (event : MeasurementEvent)  =
//         let measurement = StorableMeasurement event.Measurement
//         measurement.Value <> entry.MeasuredValue

//     let private entryAfterEvent (entry : SensorStatus) (event : MeasurementEvent) : SensorStatus =
//         let measurement = StorableMeasurement event.Measurement
//         let lastUpdated = 
//             if event |> measuredPropertyChanged entry
//             then event.Timestamp.AsDateTime
//             else entry.LastUpdated
        
//         { entry with DeviceId = event.DeviceId.AsString
//                                 MeasuredProperty = measurement.Name
//                                 MeasuredValue = measurement.Value
//                                 LastUpdated = lastUpdated
//                                 LastActive = event.Timestamp.AsDateTime }

//     let AfterEvent (event : MeasurementEvent) (SensorStatus : UpdatedSensorStatuses) : UpdatedSensorStatuses =
//         let measurement = StorableMeasurement event.Measurement

//         let isFromSameSensor (sensorStatus : SensorStatus) =
//             (sensorStatus.DeviceId = event.DeviceId.AsString) &&
//             (sensorStatus.MeasuredProperty = measurement.Name)

//         let isFromDifferentSensor (sensorStatus : SensorStatus) =
//             not(isFromSameSensor sensorStatus)

//         let entiesToBeLeftIntact = SensorStatus.Entries |> List.filter isFromDifferentSensor
//         let entryToBeUpdaterOrEmpty = SensorStatus.Entries |> List.filter isFromSameSensor

//         let entryToBeUpdated =
//             match entryToBeUpdaterOrEmpty with
//             | head::tail -> head
//             | [] -> EmptySensorStatus
        
//         let updatedEntry = event |> entryAfterEvent entryToBeUpdated
//         let allEntries = entiesToBeLeftIntact |> List.append [updatedEntry]
//         let hasChanged = event |> measuredPropertyChanged updatedEntry
        
//         { DeviceGroupId = SensorStatus.DeviceGroupId
//           Entries = allEntries
//           UpdatedEntry = updatedEntry 
//           MeasuredPropertyChanged = hasChanged }
