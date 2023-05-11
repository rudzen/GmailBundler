# GmailBundler
"Bundles" gmails up based on label(s) and generates simple CSV file from the gathered data.

## appsettings.user.json example

To allow the application, you need to configure a Google API.

- Create a file named appsettings.user.json

```json
{
  "GoogleApi": {
    "ClientId": "xxxxxxxxxxxx-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx.apps.googleusercontent.com",
    "ClientSecret": "XXXXXX-XXXXXXXXXXX-XXXXXXXXXXXXXXXX",
    "Labels": ["Github", "GmailBundler"]  
  }
}
```

Copy over the ClientId and the ClientSecret from the developer json file accessible from the google developer page.

Edit the labels you wish to process.
