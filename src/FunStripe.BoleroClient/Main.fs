module FunStripe.BoleroClient.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open FunStripe

/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Home

/// The Elmish application's model.
type Model =
    {
        page: Page
        publishableApiKey: string
        stripeObject: obj option
        cardholderName: string
        isAuthorising: bool
        paymentMethod: StripeModel.PaymentMethod option
        isTakingPayment: bool
        paymentIntent: StripeModel.PaymentIntent option
        stripeError: StripeError.ErrorObject option
        error: string option
        username: string
        password: string
        signedInAs: option<string>
        signInFailed: bool
    }

let initModel =
    {
        page = Home
        publishableApiKey = ""
        stripeObject = None
        cardholderName = ""
        isAuthorising = false
        paymentMethod = None
        isTakingPayment = false
        paymentIntent = None
        stripeError = None
        error = None
        username = ""
        password = ""
        signedInAs = None
        signInFailed = false
    }

/// Remote service definition.
type FunStripeService =
    {

        /// Get the publishable Stripe API key
        GetPublishableApiKey: unit -> Async<string>

        /// Get the setup intent client secret
        GetSetupIntent: unit -> Async<Result<StripeModel.SetupIntent, StripeError.ErrorResponse>>

        /// Get the setup intent client secret
        GetPaymentMethod: string -> Async<Result<StripeModel.PaymentMethod, StripeError.ErrorResponse>>

        /// Get the payment intent ID
        CreatePaymentIntent: string -> Async<Result<StripeModel.PaymentIntent, StripeError.ErrorResponse>>

        /// Sign into the application.
        SignIn : string * string -> Async<option<string>>

        /// Get the user's name, or None if they are not authenticated.
        GetUsername : unit -> Async<string>

        /// Sign out from the application.
        SignOut : unit -> Async<unit>
    }

    interface IRemoteService with
        member this.BasePath = "/funstripe"

/// The Elmish application's update messages.
type Message =
    | GetApiKey
    | InitStripeJs of string
    | InitedStripeJs of string
    | CreatedCardElement of string
    | SetCardholderName of string
    | Authorise
    | Authorising of Result<StripeModel.SetupIntent, StripeError.ErrorResponse>
    | Authorised of string
    | GotPaymentMethod of Result<StripeModel.PaymentMethod, StripeError.ErrorResponse>
    | TakePayment of string
    | TakingPayment of string
    | TakenPayment of Result<StripeModel.PaymentIntent, StripeError.ErrorResponse>
    | SetPage of Page
    | SetUsername of string
    | SetPassword of string
    | GetSignedInAs
    | RecvSignedInAs of option<string>
    | SendSignIn
    | RecvSignIn of option<string>
    | SendSignOut
    | RecvSignOut
    | StripeError of StripeError.ErrorObject
    | ClearStripeError
    | Error of exn
    | ClearError

let simpleError message =
    StripeError.ErrorObject.New(message = message) |> Some

let update jsRuntime remote message model =
    let onSignIn = function
        | Some _ -> Cmd.ofMsg GetApiKey
        | None -> Cmd.none

    match message with
    | GetApiKey ->
        model, Cmd.OfAsync.either remote.GetPublishableApiKey () InitStripeJs Error

    | InitStripeJs apiKey ->
        { model with publishableApiKey = apiKey }, Cmd.OfJS.either jsRuntime "initStripeJs" [| apiKey |] InitedStripeJs Error

    | InitedStripeJs _ ->
        model, Cmd.OfJS.either jsRuntime "createCardElement" [| "#Card" |] CreatedCardElement Error

    | CreatedCardElement _ ->
        model, Cmd.none

    | SetCardholderName s ->
        { model with cardholderName = s }, Cmd.none

    | Authorise ->
        { model with isAuthorising = true }, Cmd.OfAsync.either remote.GetSetupIntent () Authorising Error

    | Authorising result ->
        match result with
        | Result.Ok setupIntent when setupIntent.ClientSecret.IsSome ->
            { model with isAuthorising = false }, Cmd.OfJS.either jsRuntime "authorise" [| setupIntent.ClientSecret.Value; (model.cardholderName) |] Authorised Error
        | Result.Ok setupIntent ->
            { model with stripeError = simpleError "Client secret is null"; isAuthorising = false }, Cmd.none
        | Result.Error e ->
            { model with stripeError = Some e.StripeError; isAuthorising = false }, Cmd.none

    | Authorised paymentMethodId ->
        model, Cmd.OfAsync.either remote.GetPaymentMethod paymentMethodId GotPaymentMethod Error

    | GotPaymentMethod result ->
        match result with
        | Result.Ok paymentMethod ->
            { model with paymentMethod = Some paymentMethod }, Cmd.none
        | Result.Error e ->
            { model with paymentMethod = None; stripeError = Some e.StripeError }, Cmd.none

    | TakePayment paymentMethodId ->
        { model with isTakingPayment = true }, Cmd.ofMsg (TakingPayment paymentMethodId)

    | TakingPayment paymentMethodId ->
        model, Cmd.OfAsync.either remote.CreatePaymentIntent paymentMethodId TakenPayment Error

    | TakenPayment result ->
        match result with
        | Result.Ok paymentIntent ->
            { model with isTakingPayment = false; paymentIntent = Some paymentIntent }, Cmd.none
        | Result.Error e when e.StripeError.PaymentIntent.IsSome -> //to do: how to handle this?
            { model with isTakingPayment = false; paymentIntent = Some e.StripeError.PaymentIntent.Value }, Cmd.none
        | Result.Error e -> 
            { model with isTakingPayment = false; paymentIntent = None; stripeError = Some e.StripeError }, Cmd.none

    | SetPage page ->
        { model with page = page }, Cmd.none

    | SetUsername s ->
        { model with username = s }, Cmd.none

    | SetPassword s ->
        { model with password = s }, Cmd.none

    | GetSignedInAs ->
        model, Cmd.OfAuthorized.either remote.GetUsername () RecvSignedInAs Error

    | RecvSignedInAs username ->
        { model with signedInAs = username }, onSignIn username

    | SendSignIn ->
        model, Cmd.OfAsync.either remote.SignIn (model.username, model.password) RecvSignIn Error

    | RecvSignIn username ->
        { model with signedInAs = username; signInFailed = Option.isNone username }, onSignIn username

    | SendSignOut ->
        model, Cmd.OfAsync.either remote.SignOut () (fun () -> RecvSignOut) Error

    | RecvSignOut ->
        { model with signedInAs = None; signInFailed = false }, Cmd.none

    | StripeError se ->
        { model with stripeError = Some se }, Cmd.none

    | ClearStripeError ->
        { model with stripeError = None }, Cmd.none

    | Error RemoteUnauthorizedException ->
        { model with error = Some "You have been logged out."; signedInAs = None }, Cmd.none

    | Error exn ->
        { model with error = Some $"%A{exn}"; isAuthorising = false }, Cmd.none

    | ClearError ->
        { model with error = None }, Cmd.none

/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

type Main = Template<"wwwroot/main.html">

let homePage model (username: string) dispatch =
    Main.Home()
        .Username(username)
        .SignOut(fun _ -> dispatch SendSignOut)
        .SetCardholderName(fun e -> dispatch (SetCardholderName (e.Value.ToString())))
        .Authorise(fun _ ->
            match model.isAuthorising with
            | false -> dispatch Authorise
            | true -> ()
        )
        .SetupStatus(
            cond model.paymentMethod <| function
            | Some pm -> text $"Payment Method ID: {pm.Id}"
            | None when model.isAuthorising -> text "authorising..."
            | _ -> text "not done"
        )
        .TakePayment(fun _ ->
            match model.paymentMethod, model.isTakingPayment with
            | Some pm, false -> dispatch (TakePayment pm.Id)
            | _ -> ()
        )
        .PaymentStatus(
            cond model.paymentIntent <| function
            | Some pi -> text $"Payment intent ID: {pi.Id}; payment status: {pi.Status}"
            | None when model.isTakingPayment -> text "taking payment..."
            | None -> text "not made"
        )
        .Elt()

let signInPage model dispatch =
    Main.SignIn()
        .Username(model.username, fun s -> dispatch (SetUsername s))
        .Password(model.password, fun s -> dispatch (SetPassword s))
        .SignIn(fun _ -> dispatch SendSignIn)
        .ErrorNotification(
            cond model.signInFailed <| function
            | false -> empty
            | true ->
                Main.ErrorNotification()
                    .HideClass("is-hidden")
                    .Text("Sign in failed. Use any username and the password \"password\".")
                    .Elt()
        )
        .Elt()

let menuItem (model: Model) (page: Page) (text: string) =
    Main.MenuItem()
        .Active(if model.page = page then "is-active" else "")
        .Url(router.Link page)
        .Text(text)
        .Elt()

let view model dispatch =
    Main()
        .Menu(concat [
            menuItem model Home "Home"
        ])
        .Body(
            cond model.page <| function
            | Home -> 
                cond model.signedInAs <| function
                | Some username -> homePage model username dispatch
                | None -> signInPage model dispatch
        )
        .Error(
            cond model.stripeError <| function
            | None -> empty
            | Some err ->
                Main.ErrorNotification()
                    .Text(err.Message |> Option.defaultValue "Unknown error")
                    .Hide(fun _ -> dispatch ClearStripeError)
                    .Elt()
        )
        .Error(
            cond model.error <| function
            | None -> empty
            | Some err ->
                Main.ErrorNotification()
                    .Text(err)
                    .Hide(fun _ -> dispatch ClearError)
                    .Elt()
        )
        .Elt()

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let funStripeService = this.Remote<FunStripeService>()
        let update = update (this.JSRuntime) funStripeService
        Program.mkProgram (fun _ -> initModel, Cmd.ofMsg GetSignedInAs) update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
