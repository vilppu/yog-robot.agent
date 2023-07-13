namespace YogRobot

module Json =
    open System.Text.Json

    let jsonSerializerOptions = JsonSerializerOptions()

    jsonSerializerOptions.PropertyNameCaseInsensitive <- true

    let Serialize<'T> source : string = JsonSerializer.Serialize source

    let Deserialize<'T> (json: string) : 'T =
        JsonSerializer.Deserialize<'T>(json, jsonSerializerOptions)
