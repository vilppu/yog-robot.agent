namespace YogRobot

[<AutoOpen>]
module Authorization = 
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Security.Cryptography
    open System.Text
    open System.IdentityModel.Tokens.Jwt
    open System.Security.Claims    
    open Microsoft.AspNetCore.Authorization
    open Microsoft.AspNetCore.Http
    open Microsoft.IdentityModel.Tokens
    
    module Roles = 
        [<Literal>]
        let None = "None"
        [<Literal>]
        let Administrator = "RequiresMasterToken"
        [<Literal>]
        let User = "RequiresDeviceGroupToken"
        [<Literal>]
        let Sensor = "RequiresSensorToken"
    
    let SigningKey=
        let secretKey = StoredTokenSecret()
        SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey))

    let SecureSigningCredentials = 
        let credentials = SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        credentials
    
    let FindHeader headerName (request : HttpRequest) = 
        request.Headers
        |> Seq.toList
        |> List.filter (fun header -> header.Key = headerName)
        |> List.map (fun header -> 
               header.Value
               |> Seq.toList
               |> Seq.head)
    
    let private validMasterKeyHeaderIsPresent request =
        async {
            let headers = request |> FindHeader "yog-robot-key"
            match headers with
            | key :: tail -> 
                let now = DateTime.UtcNow
                return! IsValidMasterKeyToken (MasterKeyToken key) now
            | [] -> return false
        }
    
    let private validDeviceGroupKeyHeaderIsPresent request = 
        async {
            let key = request |> FindHeader "yog-robot-device-group-key"
            let deviceGroupIds = request |> FindHeader "yog-robot-device-group-id"
        
            let headers = 
                if key.Length = deviceGroupIds.Length then key |> List.zip deviceGroupIds
                else []
            match headers with
            | head :: tail -> 
                let (deviceGroupId, key) = head
                let now = DateTime.UtcNow
                return! IsValidDeviceGroupKeyToken (DeviceGroupId deviceGroupId) (DeviceGroupKeyToken key) now
            | [] -> return false
        }

    let private validSensorDataKeyHeaderIsPresent request = 
        async {
            let key = request |> FindHeader "yog-robot-sensor-data-key"
            let deviceGroupIdHeader = request |> FindHeader "yog-robot-device-group-id"
            let botIdIdHeader = request |> FindHeader "yog-robot-bot-id"
            let deviceGroupIds = deviceGroupIdHeader |> List.append botIdIdHeader 
        
            let headers = 
                if key.Length = deviceGroupIds.Length then key |> List.zip deviceGroupIds
                else []
            match headers with
            | head :: _ -> 
                let (deviceGroupId, key) = head
                let now = DateTime.UtcNow
                return! IsValidSensorKeyToken (DeviceGroupId deviceGroupId) (SensorKeyToken key) now
            | [] -> return false
        }
    
    let MasterKeyIsMissing request =
        async {
            let! isPresent = validMasterKeyHeaderIsPresent request
            return not(isPresent)
        }

    let DeviceGroupKeyIsMissing request =
        async {
            let! isPresent = validDeviceGroupKeyHeaderIsPresent request
            return not(isPresent)
        }
    
    let SensorKeyIsMissing request =
        async {
            let! isPresent = validSensorDataKeyHeaderIsPresent request
            return not(isPresent)
        }

    let GetDeviceGroupId(user : ClaimsPrincipal) = 
        user.Claims.Single(fun claim -> claim.Type = "DeviceGroupId").Value    

    let FindDeviceGroupId request =
        let deviceGroupIdHeader = request |> FindHeader "yog-robot-device-group-id"
        let botIdIdHeader = request |> FindHeader "yog-robot-bot-id"
        let headers = deviceGroupIdHeader |> List.append botIdIdHeader 

        headers
        |> List.map DeviceGroupId
        |> List.head

    let GenerateSecureToken() = 
        let randomNumberGenerator = RandomNumberGenerator.Create()
        let tokenBytes = Array.zeroCreate<byte> 16
        randomNumberGenerator.GetBytes tokenBytes
        let tokenWithDashes = BitConverter.ToString tokenBytes
        tokenWithDashes.Replace("-", "")    

    let BuildRoleToken role deviceGroupId = 
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        let roleClaim = Claim(ClaimTypes.Role, role)
        let deviceGroupIdClaim = Claim("DeviceGroupId", deviceGroupId)
        let claimsIdentity = ClaimsIdentity([roleClaim; deviceGroupIdClaim], "YogRobot")
        let securityTokenDescriptor = SecurityTokenDescriptor()
        securityTokenDescriptor.Subject <- claimsIdentity
        securityTokenDescriptor.SigningCredentials <- SecureSigningCredentials
        let tokenHandler = JwtSecurityTokenHandler()
        let token = tokenHandler.CreateEncodedJwt(securityTokenDescriptor)
        token   
        
    let GenerateMasterAccessToken() =
        BuildRoleToken Roles.Administrator (DeviceGroupId "")

    let GenerateDeviceGroupAccessToken deviceGroupId = 
        BuildRoleToken Roles.User deviceGroupId
    
    let GenerateSensorAccessToken deviceGroupId = 
        BuildRoleToken Roles.Sensor deviceGroupId

    let RegisterMasterKey() = 
        let key : MasterKey = 
            { Token = MasterKeyToken(GenerateSecureToken())
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        async { 
            do! StoreMasterKey key
            return key.Token
        }
    
    let RegisterDeviceGroupKey deviceGroupId = 
        let key : DeviceGroupKey = 
            { Token = DeviceGroupKeyToken(GenerateSecureToken())
              DeviceGroupId = deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        async { 
            do! StoreDeviceGroupKey key
            return key.Token
        }
    
    let RegisterSensorKey deviceGroupId = 
        let key : SensorKey = 
            { Token = SensorKeyToken(GenerateSecureToken())
              DeviceGroupId = deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        async { 
            do! StoreSensorKey key
            return key.Token
        }
    
    type PermissionRequirement(role) = 
        interface IAuthorizationRequirement
        member val Permission = role with get
    
    type PermissionHandler() = 
        inherit AuthorizationHandler<PermissionRequirement>()
        override this.HandleRequirementAsync(context : AuthorizationHandlerContext, requirement : PermissionRequirement) = 
            let isInRequiredRole = context.User.IsInRole requirement.Permission
           
            if isInRequiredRole then
                context.Succeed requirement
                
            System.Threading.Tasks.Task.FromResult(0) :> System.Threading.Tasks.Task
