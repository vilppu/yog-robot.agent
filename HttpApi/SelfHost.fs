namespace YogRobot

// netsh http add urlacl http://localhost:18888/ user=user
// netsh http add urlacl http://127.0.0.1:18888/ user=user
// netsh http add urlacl http://xxx.xxx.xxx.xxx:18888/ user=user
// netsh http add urlacl http://169.254.80.80:18888/ user=user
[<AutoOpen>]
module SelfHost = 
    open System
    open System.IO
    open System.Threading
    open System.Threading.Tasks    
    open Microsoft.AspNetCore.Authorization
    open Microsoft.AspNetCore.Authentication.JwtBearer
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Hosting
    open Microsoft.AspNetCore.Mvc
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Logging
    open Microsoft.Extensions.Configuration
    open Microsoft.IdentityModel.Tokens
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization

    type HttpService() =
        interface IHttpService with 
            member this.Send request = Http.Send request
    
    type Startup(environment : IHostingEnvironment) =       
        
        let getConfiguration() =
                (ConfigurationBuilder())
                 .SetBasePath(environment.ContentRootPath)
                 .AddEnvironmentVariables()
                 .Build()
        
        member this.Configure(app : IApplicationBuilder, env : IHostingEnvironment, loggerFactory : ILoggerFactory) =             
            loggerFactory
                .AddConsole(LogLevel.Warning)
                .AddDebug()
                |> ignore
                
            let jwtBearerOptions =
                let tokenValidationParameters = TokenValidationParameters()
                tokenValidationParameters.ValidateIssuerSigningKey <- true
                tokenValidationParameters.IssuerSigningKey <- SigningKey
                tokenValidationParameters.ClockSkew <- TimeSpan.Zero
                tokenValidationParameters.ValidateIssuer <- false
                tokenValidationParameters.ValidIssuer <- "NotUsed"
                tokenValidationParameters.ValidateAudience <- false
                tokenValidationParameters.ValidAudience <- "NotUsed"
                tokenValidationParameters.ValidateLifetime <- false
                let options = JwtBearerOptions()
                options.AutomaticAuthenticate <- true
                options.AutomaticChallenge <- true
                options.TokenValidationParameters <- tokenValidationParameters
                options

            app.UseJwtBearerAuthentication(jwtBearerOptions).UseMvc()
            |> ignore
            
        member this.ConfigureServices(services : IServiceCollection) =
            let configureJson (options : MvcJsonOptions) = 
                options.SerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
            let configureJsonAction = new Action<MvcJsonOptions>(configureJson)
            services
                .AddMvc()
                .AddJsonOptions(configureJsonAction)
                |> ignore

            services.AddSingleton<IHttpService, HttpService>() |> ignore
            
            let configureAdminPolicy =
                let builder =
                    fun (policy : AuthorizationPolicyBuilder) ->
                        policy.Requirements.Add(PermissionRequirement(Roles.Administrator))
                new Action<AuthorizationPolicyBuilder>(builder)

            let configureUserPolicy =
                let builder =
                    fun (policy : AuthorizationPolicyBuilder) ->
                        policy.Requirements.Add(PermissionRequirement(Roles.User))
                new Action<AuthorizationPolicyBuilder>(builder)

            let configureSensorPolicy =
                let builder =
                    fun (policy : AuthorizationPolicyBuilder) ->
                        policy.Requirements.Add(PermissionRequirement(Roles.Sensor))
                new Action<AuthorizationPolicyBuilder>(builder)

            
            services.AddAuthorization(fun options ->
                options.AddPolicy(Roles.Administrator, configureAdminPolicy)
                options.AddPolicy(Roles.User, configureUserPolicy)
                options.AddPolicy(Roles.Sensor, configureSensorPolicy)
            ) |> ignore

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>()
            |> ignore

    let CreateHttpServer() : Task = 
        let url = Environment.GetEnvironmentVariable("YOG_BOT_BASE_URL")
        
        let url = 
            if String.IsNullOrWhiteSpace(url) then "http://localhost:18888/yog-robot/"
            else url

        let host = 
            WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(url)
                .Build()

        let tokenSource = new CancellationTokenSource()
        let token = tokenSource.Token
        Task.Run(fun () -> host.Run())
        
