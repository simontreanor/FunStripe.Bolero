namespace FunStripe.BoleroServer

open Microsoft.AspNetCore.Hosting
open Bolero.Remoting.Server
open System

type FunStripeService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<FunStripe.BoleroClient.Main.FunStripeService>()

    override this.Handler =
        {

            GetPublishableApiKey = ctx.Authorize <| fun () -> async {
                return Config.stripePublicTestApiKey
            }

            GetSetupIntent = ctx.Authorize <| fun () -> async {
                let customerId = ctx.HttpContext.User.Identity.Name
                return! ScaWorkflow.createSetupIntent customerId
            }

            GetPaymentMethod = ctx.Authorize <| fun paymentMethodId -> async {
                return! ScaWorkflow.getPaymentMethod paymentMethodId
            }

            CreatePaymentIntent = ctx.Authorize <| fun paymentMethodId -> async {
                let customerId = ctx.HttpContext.User.Identity.Name
                return! ScaWorkflow.createPaymentIntent customerId paymentMethodId
            }

            SignIn = fun (username, password) -> async {
                if password = "password" then
                    do! ctx.HttpContext.AsyncSignIn(username, TimeSpan.FromDays(365.))
                    return Some username
                else
                    return None
            }

            SignOut = fun () -> async {
                return! ctx.HttpContext.AsyncSignOut()
            }

            GetUsername = ctx.Authorize <| fun () -> async {
                return ctx.HttpContext.User.Identity.Name
            }
        }
