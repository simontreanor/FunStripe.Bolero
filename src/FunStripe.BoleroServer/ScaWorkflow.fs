namespace FunStripe.BoleroServer

open FunStripe
open FunStripe.AsyncResultCE
open FunStripe.RestApi
open FunStripe.StripeError
open FunStripe.StripeRequest

///(Based on Stripe docs: Set up future payments)[https://stripe.com/docs/payments/save-and-reuse#web-setup]
module ScaWorkflow =

    let settings = StripeApiSettings.New(apiKey = Config.stripeSecretTestApiKey)

    let simpleError message =
        async { return Error { ErrorResponse.StripeError = ErrorObject.New(message = message) } }

    let createSetupIntent (customerId: string) =
        asyncResult {
            return!
                SetupIntents.CreateOptions.New(
                    customer = customerId,
                    usage = SetupIntents.Create'Usage.OffSession
                )
                |> SetupIntents.Create settings
        }

    let getPaymentMethod (paymentMethodId: string) =
        asyncResult {
            return!
                PaymentMethods.RetrieveOptions.New(
                    paymentMethod = paymentMethodId
                )
                |> PaymentMethods.Retrieve settings
        }

    let createPaymentIntent (customerId: string)  (paymentMethodId: string) =
        asyncResult {
            //Stripe docs recommend listing the customer's payment methods rather than using the paymentMethodId directly, but don't say why
            let! paymentMethods =
                PaymentMethods.ListOptions.New(
                    customer = customerId,
                    type' = "card"
                )
                |> PaymentMethods.List settings
            let paymentMethod =
                paymentMethods
                |> List.tryFind(fun pm -> pm.Id = paymentMethodId)
            match paymentMethod with
            | Some pm ->
                return!
                    PaymentIntents.CreateOptions.New (
                        amount = 12500,
                        confirm = true,
                        currency = "GBP",
                        customer = customerId,
                        offSession = Choice1Of2 true,
                        paymentMethod = pm.Id
                    )
                    |> PaymentIntents.Create settings
            | None ->
                return! simpleError "No matching payment method found"
        }
