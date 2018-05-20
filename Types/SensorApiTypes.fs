namespace YogRobot

module SensorApiTypes =

    type SensorDatum = 
        { name : string
          value : string
          scale : int
          formattedValue : string }
    
    type SensorData = 
        { event : string
          gatewayId : string
          channel : string
          sensorId : string
          data : SensorDatum list
          batteryVoltage : string
          rssi : string }
          
    type SensorHistoryResultEntry = 
        { MeasuredValue : obj
          Timestamp : System.DateTime }
    
    type SensorHistoryResult = 
        { SensorId : string
          MeasuredProperty : string
          Entries : SensorHistoryResultEntry list }

    type SensorStatusResult = 
        { DeviceGroupId : string
          DeviceId : string
          SensorId : string
          SensorName : string
          MeasuredProperty : string
          MeasuredValue : obj
          BatteryVoltage : float
          SignalStrength : float
          LastUpdated : System.DateTime
          LastActive : System.DateTime }
