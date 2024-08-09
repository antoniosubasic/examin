# examin

## Overview
**examin** is a project I've been working on to make it easier to keep track of your exams by automatically syncing them with your Google Calendar. The state of the app is still pretty early in development, so there are a few rough edges and bugs to be expected.

## Features
The app connects to your school's _WebUntis Server_ (you'll need to enter your login details) and grabs all your exams for a specified timeframe, enabling you to then sync them with your Google Calendar using one of the following options:

1. Manual CSV Import
The app allows you to export the exams as a _.csv_ file and then manually import them to Google Calendar (see: [Documentation - Import events to Google Calendar](https://support.google.com/calendar/answer/37118). This comes with the benefit of the user not having to deal with any **OAuth consent screen** setup in the Google Cloud Console for the API.
2. Automated Google Calendar Sync with OAuth
For those who prefer a fully automated process, this option will push your exams directly to your Google Calendar using the Google API. To use this method, you'll first need to set up the **OAuth consent screen** (see: [Documentation - Configure the OAuth consent screen and choose scopes](https://developers.google.com/workspace/guides/configure-oauth-consent)) and **Create access credentials** (see: [Documentation - Create access credentials](https://developers.google.com/workspace/guides/create-credentials)). For both the scopes `.../auth/calendar.events` and `.../auth/calendar.events.owned` are needed. Once everything is set up, examin will use the `client_secrets.json` file to authenticate with Google's API. After the initial authorization via the OAuth consent screen, your exams will be automatically pushed to your Google Calendar without any further action required from you.

## Issues

- **Basic UI**: The interface is pretty bare-bones right now. I've got plans to improve it, but for now, it's functional, if not fancy.
- **Potential Bugs**: Even though the app has been thoroughly tested, it's still in the early stages of development, so there may be unexpected bugs that occur. If you encounter any issues, please let me know.

## Security Considerations !!!
**No Encryption Yet**: Sensitive data (like `client_secrets.json` or `calendarID`) isn't being encrypted at the moment.

## What's Next?

- **Security Upgrades**: My top priority right now is to secure how your credentials and secrets are stored - probably by adding encryption.
- **Bug Fixes**: I'm on it! Fixing bugs is a big focus as I keep developing.
- **More Features**: I'm planning on adding better calendar integration (like automatic detecting of already added exams) and slight UI improvements.

<br>
<br>
<br>

Since this is a solo project, your feedback is super valuable! If you find any bugs or have ideas for new features, feel free to open an issue on GitHub or send in a pull request.