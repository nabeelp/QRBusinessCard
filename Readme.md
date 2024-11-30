# QR Business Card

This repository contains the source code for a QR Business Card generator. The application allows users to create a digital business card with a QR code that can be scanned to share contact information easily.

## Features

- Generate QR codes for business cards
- Customize contact information
- Allow for business cards to be save to Google and Apple Wallets

## Google Wallet Notes

- Google Wallet passes are generated using the Goole Wallet REST API and require a Google Developer account.
- The Google Wallet API is used to generate a JSON object that represents the pass.
- The JSON object is then converted to a JWT token and signed with a private key.
- The JWT token is then sent to the Google Wallet API to generate the pass.

To be able to use the Google Wallet REST API, you will need to create a Google Developer account and create a project. You will also need to create a service account and download the private key file. This solution utilises [Azure Key Vault](https://azure.microsoft.com/en-us/products/key-vault) to store the private key securely.

For more information on how to create a Google Wallet pass, see the [Google Wallet API documentation](https://developers.google.com/pay/passes/guides/get-started/implementing-the-api/define-google-pay-api-objects).

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

