namespace YogRobot

open Microsoft.AspNetCore.Authentication.JwtBearer
open FirebaseAdmin.Messaging
open Microsoft.AspNetCore

[<AutoOpen>]
module SelfHost =
    open System
    open System.IO
    open System.Threading.Tasks
    open Microsoft.AspNetCore.Authorization
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Hosting
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Logging
    open Microsoft.IdentityModel.Tokens
    open System.Text.Json.Serialization

    let private GetUrl () =

        let configuredUrl = Environment.GetEnvironmentVariable("YOG_BOT_BASE_URL")

        let url =
            if String.IsNullOrWhiteSpace(configuredUrl) then
                "http://localhost:18888/yog-robot"
            else
                configuredUrl

        new Uri(url)

    type Startup(environment: IWebHostEnvironment) =

        member this.Configure
            (
                app: IApplicationBuilder,
                env: IWebHostEnvironment,
                loggerFactory: ILoggerFactory,
                sendFirebaseMulticastMessages: MulticastMessage -> Task<BatchResponse>
            ) =

            app
                .UsePathBase(new Microsoft.AspNetCore.Http.PathString(GetUrl().PathAndQuery))
                .UseAuthentication()
                //.UseCors(fun options ->
                //    options
                //     .AllowAnyOrigin()
                //     .AllowAnyMethod()
                //     .AllowAnyHeader()
                //     .AllowCredentials()|> ignore)
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(fun endpoints -> endpoints.MapControllers() |> ignore)
            |> ignore

        member this.ConfigureServices(services: IServiceCollection) =


            services
                .AddControllers()
                .AddJsonOptions(fun options ->
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive <- true
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            |> ignore

            let configureAdminPolicy =
                let builder =
                    fun (policy: AuthorizationPolicyBuilder) ->
                        policy.Requirements.Add(PermissionRequirement(Roles.Administrator))

                new Action<AuthorizationPolicyBuilder>(builder)

            let configureUserPolicy =
                let builder =
                    fun (policy: AuthorizationPolicyBuilder) ->
                        policy.Requirements.Add(PermissionRequirement(Roles.User))

                new Action<AuthorizationPolicyBuilder>(builder)

            let configureSensorPolicy =
                let builder =
                    fun (policy: AuthorizationPolicyBuilder) ->
                        policy.Requirements.Add(PermissionRequirement(Roles.Sensor))

                new Action<AuthorizationPolicyBuilder>(builder)

            services.AddAuthorization(fun options ->
                options.AddPolicy(Roles.Administrator, configureAdminPolicy)
                options.AddPolicy(Roles.User, configureUserPolicy)
                options.AddPolicy(Roles.Sensor, configureSensorPolicy))
            |> ignore


            let tokenValidationParameters = TokenValidationParameters()
            tokenValidationParameters.ValidateIssuerSigningKey <- true
            tokenValidationParameters.IssuerSigningKey <- SigningKey
            tokenValidationParameters.ClockSkew <- TimeSpan.Zero
            tokenValidationParameters.ValidateIssuer <- false
            tokenValidationParameters.ValidIssuer <- "NotUsed"
            tokenValidationParameters.ValidateAudience <- false
            tokenValidationParameters.ValidAudience <- "NotUsed"
            tokenValidationParameters.ValidateLifetime <- false

            services
                .AddAuthentication(fun options ->
                    options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                    options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(fun options -> options.TokenValidationParameters <- tokenValidationParameters)
            |> ignore

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>() |> ignore

    let CreateHttpServer (sendFirebaseMulticastMessages: MulticastMessage -> Task<BatchResponse>) : Task =

        let url = GetUrl()

        let host = url.Scheme + Uri.SchemeDelimiter + url.Host + ":" + url.Port.ToString()

        let host =
            WebHost
                .CreateDefaultBuilder()
                .ConfigureServices(fun services -> services.AddSingleton(sendFirebaseMulticastMessages) |> ignore)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(host)
                .Build()

        Task.Run(fun () -> host.Run())
