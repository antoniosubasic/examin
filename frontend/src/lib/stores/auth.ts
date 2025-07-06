import { writable } from 'svelte/store';
import { goto } from '$app/navigation';
import { browser } from '$app/environment';

interface AuthState {
    sessionId: string;
    schoolName: string;
    isAuthenticated: boolean;
}

function createAuthStore() {
    const { subscribe, set, update } = writable<AuthState>({
        sessionId: '',
        schoolName: '',
        isAuthenticated: false
    });

    return {
        subscribe,

        init: () => {
            if (browser) {
                const sessionId = localStorage.getItem('sessionId') || '';
                const schoolName = localStorage.getItem('schoolName') || '';

                set({
                    sessionId,
                    schoolName,
                    isAuthenticated: !!sessionId
                });
            }
        },

        login: (sessionId: string, schoolName: string) => {
            if (browser) {
                localStorage.setItem('sessionId', sessionId);
                localStorage.setItem('schoolName', schoolName);
            }

            set({
                sessionId,
                schoolName,
                isAuthenticated: true
            });
        },

        logout: () => {
            if (browser) {
                localStorage.removeItem('sessionId');
                localStorage.removeItem('schoolName');
                localStorage.removeItem('justLoggedIn');
            }

            set({
                sessionId: '',
                schoolName: '',
                isAuthenticated: false
            });

            goto('/');
        },

        updateSession: (sessionId: string, schoolName: string) => {
            update(state => ({
                ...state,
                sessionId,
                schoolName
            }));
        }
    };
}

export const authStore = createAuthStore();
