namespace YogRobot

[<AutoOpen>]
module Sensors = 
    type GatewayEvent = 
        | GatewayUpEvent of SensorData
        | GatewayDownEvent of SensorData
        | GatewayActiveOnChannelEvent of SensorData
        | SensorUpEvent of SensorData
        | SensorDataEvent of SensorData
    
    let ToSensorEvent(sensorData : SensorData) = 
        match sensorData.event with
        | "gateway up" -> GatewayUpEvent sensorData
        | "gateway down" -> GatewayDownEvent sensorData
        | "gateway active" -> GatewayActiveOnChannelEvent sensorData
        | "sensor up" -> SensorUpEvent sensorData
        | "sensor data" -> SensorDataEvent sensorData
        | _ -> failwith ("unknown sensor event: " + sensorData.event)
    
    let ToAnySensorEvent gatewayEvent = 
        match gatewayEvent with
        | GatewayEvent.GatewayUpEvent event -> event
        | GatewayEvent.GatewayDownEvent event -> event
        | GatewayEvent.GatewayActiveOnChannelEvent event -> event
        | GatewayEvent.SensorUpEvent event -> event
        | GatewayEvent.SensorDataEvent event -> event
