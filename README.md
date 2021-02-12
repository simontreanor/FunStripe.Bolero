# FunStripe.Bolero

A minimal F# 5.0 app demonstrating the use of [Bolero](https://fsbolero.io/) to connect to the [Stripe API](https://stripe.com/docs/api) using the [FunStripe](https://github.com/simontreanor/FunStripe) library to demonstrate the new [SCA](https://stripe.com/en-gb/guides/strong-customer-authentication)-compliant process for collecting a customer's card payment details and taking a payment.

## Installation

Instructions here are for Visual Studio Code:

1. Clone this repository
1. Open the terminal and run ```dotnet build``` to download dependencies and build the solution

## Running the demo

1. Type ```cd src\*r``` to change to the `src\FunStripe.BoleroServer` directory
1. Run ```dotnet run``` to run the app
1. Open your browser to [https://localhost:5001]

1. Log in using a valid customer ID (`cus_...`) from your Stripe test account
1. Use the password `password`
1. Click `Sign in`

1. Enter any name
1. Refer to [Set up future payments](https://stripe.com/docs/payments/save-and-reuse), `Custom payment flow`, `6. Test the integration`, for a list of possible test card numbers
1. Use any CVC, postal code, and future expiration date

1. Click `Authorise` to check the card details and get a payment method ID
1. Click `Take Payment` to take a fixed payment of £125 and get the payment intent ID and status
1. Optional: click `Take Payment` again if you wish to take another payment (a new payment intent ID is returned)

Note that as long as you are using a test API key and test card numbers then no actual payment will be taken!
