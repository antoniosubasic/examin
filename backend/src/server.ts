import express, { Request, Response } from 'express';
import dotenv from 'dotenv';
import { authorize } from './google';
import path from 'path';

dotenv.config({ path: path.join(__dirname, '../secrets/.env') });

const app = express();
const PORT = process.env.PORT || 3000;

app.get('/auth', async (_: Request, res: Response) => {
    try {
        await authorize();
        res.status(200).send('authentication successful');
    } catch (error) {
        res.status(500).send('authentication failed');
    }
});

app.listen(PORT, () => {
    console.log(`running on port ${PORT}`);
});