import express, { Request, Response } from 'express';
import dotenv from 'dotenv';
import { authorize, getEvents } from './google';
import path from 'path';
import { parse } from 'date-fns';

dotenv.config({ path: path.join(__dirname, '../secrets/.env') });

const app = express();
const PORT = process.env.PORT || 3000;
const DATE_FORMAT = process.env.DATE_FORMAT || 'yyyy-MM-dd';

app.get('/google/auth', async (_: Request, res: Response) => {
    try {
        await authorize();
        res.status(200).send('authentication successful');
    } catch (error) {
        res.status(500).send('authentication failed');
    }
});

app.get('/google/calendar/events', async (req: Request, res: Response) => {
    try {
        const { calendarId, from, to } = req.query;

        const fromDate = parse(from as string, DATE_FORMAT, new Date());
        const toDate = parse(to as string, DATE_FORMAT, new Date());

        if (isNaN(fromDate.getTime()) || isNaN(toDate.getTime())) {
            res.status(400).send('invalid date format');
            return;
        }

        const events = await getEvents(calendarId as string, fromDate, toDate);
        res.status(200).send(events);
    } catch (error) {
        res.status(500).send('failed to fetch events');
    }
});

app.listen(PORT, () => {
    console.log(`running on port ${PORT}`);
});