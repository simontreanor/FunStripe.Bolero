namespace FunStripe.BoleroServer

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Configuration.UserSecrets

module Config =
    // Look up the `UserSecrets` store on the developer's computer (change the GUID!) and retrieve the Stripe test API keys (see README for link to documentation)
    let userSecrets = ConfigurationBuilder().AddUserSecrets("170450ff-243d-4b38-9f56-c74254e1ca70").Build()
    let stripePublicTestApiKey = userSecrets.["StripePK-Test"] |> string
    let stripeSecretTestApiKey = userSecrets.["StripeSK-Test"] |> string
    
    // Instead of using user secrets, you could just specify the API keys directly as strings, as long as you don't store them in a public repo:
    //let stripePublicTestApiKey = "<Stripe public test API key>"
    //let stripeSecretTestApiKey = "<Stripe secret test API key>"
