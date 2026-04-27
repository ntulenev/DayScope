# Google Cloud setup for DayScope

This guide explains how to create your own Google Cloud project so DayScope can read your Google Calendar events and Gmail unread inbox count.

DayScope is a local Windows desktop app. It does not need a server, service account, redirect domain, or deployed Google Cloud runtime. It needs only a Google Cloud project with enabled APIs and an OAuth client of type Desktop app.

## What DayScope Uses

DayScope requests these Google API scopes:

- `https://www.googleapis.com/auth/calendar.readonly`
- `https://www.googleapis.com/auth/gmail.readonly`

The default local configuration expects the OAuth client JSON here:

```text
%LocalAppData%\DayScope\google-oauth-client.json
```

OAuth tokens are cached here:

```text
%LocalAppData%\DayScope\GoogleCalendarToken
```

The app settings are in:

```text
src\DayScope\appsettings.json
```

## 1. Create a Google Cloud Project

1. Open [Google Cloud Console](https://console.cloud.google.com/).
2. Open the project selector at the top of the page.
3. Click `New Project`.
4. Enter a name, for example `DayScope`.
5. Click `Create`.
6. Make sure the new project is selected before continuing.

## 2. Enable the Required APIs

1. In Google Cloud Console, open `APIs & Services` -> `Library`.
2. Search for `Google Calendar API`.
3. Open it and click `Enable`.
4. Go back to the API Library.
5. Search for `Gmail API`.
6. Open it and click `Enable`.

Both APIs must be enabled in the same project where you create the OAuth client.

## 3. Configure the OAuth Consent Screen

Google may now show this area as `Google Auth Platform` instead of the older `OAuth consent screen` name.

1. Open `APIs & Services` -> `OAuth consent screen`, or `Google Auth Platform` -> `Branding`.
2. If Google says the Auth Platform is not configured, click `Get Started`.
3. Set `App name` to `DayScope`.
4. Select your email as the user support email.
5. Choose the audience:
   - Use `External` for a personal Google account or if you are not inside a Google Workspace organization.
   - Use `Internal` only if this project belongs to a Google Workspace organization and only organization users should sign in.
6. Enter your email as the developer contact email.
7. Accept the Google API Services User Data Policy if you agree with it.
8. Create/save the consent screen.

For a personal/local setup, keeping the app in `Testing` is usually enough. If the app is in testing mode, add your Google account under `Audience` -> `Test users`. Google currently limits testing apps to listed test users and may expire test authorizations after 7 days for scopes that use offline access.

If you publish the app `In production`, Google can show an unverified app warning or require verification because Gmail read-only access is a sensitive/restricted Workspace scope. For your own local use, testing mode is usually simpler.

## 4. Add OAuth Scopes

1. Open `Google Auth Platform` -> `Data Access`, or the consent screen's scopes section.
2. Click `Add or remove scopes`.
3. Add these scopes:

```text
https://www.googleapis.com/auth/calendar.readonly
https://www.googleapis.com/auth/gmail.readonly
```

If the scope picker does not show them, use manual scope entry if available. Make sure Calendar API and Gmail API are enabled first, because Google only lists scopes for enabled APIs.

## 5. Create a Desktop OAuth Client

1. Open `APIs & Services` -> `Credentials`, or `Google Auth Platform` -> `Clients`.
2. Click `Create credentials` -> `OAuth client ID`, or `Create client`.
3. Choose application type `Desktop app`.
4. Name it `DayScope Desktop`.
5. Click `Create`.
6. Download the JSON file.

Do not create a `Web application` client for DayScope. A web client needs redirect URI setup and will commonly fail with `redirect_uri_mismatch` in this app. The desktop client is the expected type for the Google installed-app OAuth flow.

## 6. Put the Client JSON Where DayScope Expects It

Create the DayScope local app data directory:

```powershell
New-Item -ItemType Directory -Force "$env:LocalAppData\DayScope"
```

Copy the downloaded OAuth client JSON to:

```powershell
Copy-Item "C:\Path\To\downloaded-client.json" "$env:LocalAppData\DayScope\google-oauth-client.json"
```

Replace `C:\Path\To\downloaded-client.json` with the real path to the downloaded file.

The file must remain JSON and should contain an `installed` section. That is how Google's desktop OAuth client JSON is shaped.

## 7. Check DayScope Configuration

Open:

```text
src\DayScope\appsettings.json
```

For Google integration, these settings should look like this:

```json
{
  "DemoMode": {
    "Enabled": false
  },
  "GoogleCalendar": {
    "Enabled": true,
    "CalendarId": "primary",
    "ClientSecretsPath": "%LocalAppData%\\DayScope\\google-oauth-client.json",
    "TokenStoreDirectory": "%LocalAppData%\\DayScope\\GoogleCalendarToken",
    "ForceAccountSelection": true
  }
}
```

You can set `GoogleCalendar.LoginHint` to your email if you want Google to preselect an account:

```json
"LoginHint": "your.email@gmail.com"
```

Leave `CalendarId` as `primary` unless you intentionally want another calendar ID.

## 8. Run the App and Sign In

From the repository root:

```powershell
dotnet build .\src\DayScope.slnx
dotnet run --project .\src\DayScope\DayScope.csproj
```

On first run, DayScope opens a browser sign-in flow. Sign in with a Google account that is allowed by the OAuth audience/test-user settings, then approve Calendar read-only and Gmail read-only access.

After approval, DayScope stores tokens in:

```text
%LocalAppData%\DayScope\GoogleCalendarToken
```

Future runs should reuse that token cache.

## Troubleshooting

### The app says the client secret file is missing

Check that the file exists at:

```text
%LocalAppData%\DayScope\google-oauth-client.json
```

In PowerShell you can verify the expanded path:

```powershell
Test-Path "$env:LocalAppData\DayScope\google-oauth-client.json"
```

### Google says access is blocked or the app is not verified

For local use, keep the app in `Testing` and add your own Google account as a test user. If you are using a Google Workspace account, your organization administrator may also need to allow this OAuth app or its requested Gmail/Calendar scopes.

### Google shows `redirect_uri_mismatch`

Create a new OAuth client of type `Desktop app` and download its JSON. Do not use a `Web application` OAuth client.

### DayScope signs into the wrong Google account

Set this in `src\DayScope\appsettings.json`:

```json
"LoginHint": "your.email@gmail.com",
"ForceAccountSelection": true
```

Then delete the existing token cache and run again:

```powershell
Remove-Item -Recurse -Force "$env:LocalAppData\DayScope\GoogleCalendarToken"
```

### You changed scopes or switched accounts

Delete the token cache so DayScope asks Google for consent again:

```powershell
Remove-Item -Recurse -Force "$env:LocalAppData\DayScope\GoogleCalendarToken"
```

### You only want to try the UI without Google

Set demo mode in `src\DayScope\appsettings.json`:

```json
"DemoMode": {
  "Enabled": true
}
```

With demo mode enabled, DayScope uses synthetic calendar and inbox data and does not need Google Cloud setup.

## Official Google References

- [Configure the OAuth consent screen and choose scopes](https://developers.google.com/workspace/guides/configure-oauth-consent)
- [OAuth 2.0 for installed and desktop apps](https://developers.google.com/identity/protocols/oauth2/native-app)
- [Google API Console OAuth setup help](https://support.google.com/googleapi/answer/6158849)
- [Google Cloud OAuth app audience and testing status](https://support.google.com/cloud/answer/15549945)
