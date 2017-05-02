namespace YogRobot

[<AutoOpen>]
module Sensors = 
    type GatewayEvent = 
        | GatewayUpEvent of SensorData
        | GatewayDownEvent of SensorData
        | GatewayActiveOnChannelEvent of SensorData
        | SensorUpEvent of SensorData
        | SensorDataEvent of SensorData
    
    let ToSensorEvent(sensorEvent : SensorData) = 
        match sensorEvent.event with
        | "gateway up" -> GatewayUpEvent sensorEvent
        | "gateway down" -> GatewayDownEvent sensorEvent
        | "gateway active" -> GatewayActiveOnChannelEvent sensorEvent
        | "sensor up" -> SensorUpEvent sensorEvent
        | "sensor data" -> SensorDataEvent sensorEvent
        | _ -> failwith ("unknown sensor event: " + sensorEvent.event)
    
    let ToAnySensorEvent sensorEvent = 
        match sensorEvent with
        | GatewayEvent.GatewayUpEvent event -> event
        | GatewayEvent.GatewayDownEvent event -> event
        | GatewayEvent.GatewayActiveOnChannelEvent event -> event
        | GatewayEvent.SensorUpEvent event -> event
        | GatewayEvent.SensorDataEvent event -> event
