// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}

	interface School {
		server: string;
		displayName: string;
		loginName: string;
	}

	interface Exam {
		examType?: string;
		name?: string;
		examDate: string;
		startTime: string;
		endTime: string;
		subject?: string;
		text?: string;
	}
}

export { };
