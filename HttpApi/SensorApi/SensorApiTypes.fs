namespace YogRobot

[<AutoOpen>]
module SensorApiTypes =

    [<CLIMutable>] 
    type SensorDatum = 
        { name : string
          value : string
          scale : int
          formattedValue : string }
    
    [<CLIMutable>] 
    type SensorData = 
        { event : string
          gatewayId : string
          channel : string
          sensorId : string
          data : SensorDatum list
          batteryVoltage : string
          rssi : string }
