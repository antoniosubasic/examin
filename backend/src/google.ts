import { google } from 'googleapis';
import fs from 'fs/promises';
import path from 'path';
import { authenticate } from '@google-cloud/local-auth';

const SCOPES = ['https://www.googleapis.com/auth/calendar.events', 'https://www.googleapis.com/auth/calendar.events.owned'];
const TOKEN_PATH = path.join(__dirname, '../secrets/token.json');
const CREDENTIALS_PATH = path.join(__dirname, '../secrets/credentials.json');

async function loadSavedCredentialsIfExist() {
    try {
        const content = await fs.readFile(TOKEN_PATH);
        const credentials = JSON.parse(content.toString());
        return google.auth.fromJSON(credentials);
    } catch (err) {
        return null;
    }
}

async function saveCredentials(client: any) {
    const content = await fs.readFile(CREDENTIALS_PATH);

    const keys = JSON.parse(content.toString());
    const key = keys.installed || keys.web;

    const payload = JSON.stringify({
        type: 'authorized_user',
        client_id: key.client_id,
        client_secret: key.client_secret,
        refresh_token: client.credentials.refresh_token,
    });

    await fs.writeFile(TOKEN_PATH, payload);
}

export async function authorize() {
    const jsonClient = await loadSavedCredentialsIfExist();
    if (jsonClient) {
        return jsonClient;
    }

    const oAuth2Client = await authenticate({
        scopes: SCOPES,
        keyfilePath: CREDENTIALS_PATH,
    });

    if (oAuth2Client.credentials) {
        await saveCredentials(oAuth2Client);
    }

    return oAuth2Client;
}

export async function getEvents(calendarId: string, from: Date, to: Date) {
    const auth = await authorize();

    const calendar = google.calendar({ version: 'v3', auth: auth as any });
    const events = await calendar.events.list({
        calendarId,
        timeMin: from.toISOString(),
        timeMax: to.toISOString(),
        maxResults: 2500,
        singleEvents: true,
        orderBy: 'startTime',
    });

    return events.data;
}
