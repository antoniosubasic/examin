<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { onMount } from "svelte";

    let schoolName = "";
    let schoolLoginName = "";
    let username = "";
    let password = "";
    let loading = false;
    let error = "";

    onMount(() => {
        if (localStorage.getItem("sessionId") || "") {
            goto("/dashboard");
        }

        schoolName = page.url.searchParams.get("name") || "";
        schoolLoginName = page.url.searchParams.get("school") || "";

        if (!schoolLoginName) {
            goto("/");
        }
    });

    async function handleLogin() {
        if (!username.trim() || !password.trim()) {
            error = "Please enter both username and password";
            return;
        }

        loading = true;
        error = "";

        try {
            const response = await fetch("/api/auth/login", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    school: schoolLoginName,
                    username: username.trim(),
                    password: password,
                }),
            });

            const result = await response.json();

            if (response.ok && result.success) {
                localStorage.setItem("sessionId", result.sessionId);
                localStorage.setItem("schoolName", schoolName);
                localStorage.setItem("justLoggedIn", "true");
                goto("/dashboard");
            } else {
                error =
                    result.message ||
                    "Login failed. Please check your credentials.";
            }
        } catch (err) {
            error = "Login failed. Please try again.";
            console.error("Login error:", err);
        } finally {
            loading = false;
        }
    }

    function handleKeyPress(event: KeyboardEvent) {
        if (event.key === "Enter") {
            handleLogin();
        }
    }

    function goBack() {
        goto("/");
    }
</script>

<div
    class="min-h-screen bg-gray-50 flex items-center justify-center px-4 sm:px-6 lg:px-8"
>
    <div class="max-w-md w-full">
        <button
            on:click={goBack}
            class="inline-flex items-center text-blue-600 hover:text-blue-800 mb-8 cursor-pointer"
        >
            <svg
                class="w-4 h-4 mr-1"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
            >
                <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M15 19l-7-7 7-7"
                ></path>
            </svg>
            Back to search
        </button>
        <div class="text-center mb-8">
            <h1 class="text-3xl font-bold text-gray-900 mb-2">Login</h1>
            <p class="text-gray-600">{schoolName}</p>
        </div>

        <div class="bg-white rounded-lg shadow-md p-6">
            <form on:submit|preventDefault={handleLogin} class="space-y-4">
                <div>
                    <label
                        for="username"
                        class="block text-sm font-medium text-gray-700 mb-2"
                    >
                        Username
                    </label>
                    <input
                        id="username"
                        type="text"
                        bind:value={username}
                        on:keypress={handleKeyPress}
                        placeholder="Enter your username"
                        class="
                            w-full border border-gray-300 rounded-md px-3 py-2
                            focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                        "
                        disabled={loading}
                        required
                    />
                </div>

                <div>
                    <label
                        for="password"
                        class="block text-sm font-medium text-gray-700 mb-2"
                    >
                        Password
                    </label>
                    <input
                        id="password"
                        type="password"
                        bind:value={password}
                        on:keypress={handleKeyPress}
                        placeholder="Enter your password"
                        class="
                            w-full border border-gray-300 rounded-md px-3 py-2
                            focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                        "
                        disabled={loading}
                        required
                    />
                </div>

                {#if error}
                    <div
                        class="p-3 bg-red-100 border border-red-400 text-red-700 rounded"
                    >
                        {error}
                    </div>
                {/if}

                <button
                    type="submit"
                    disabled={loading || !username.trim() || !password.trim()}
                    class="
                        w-full py-2 px-4 mt-4 bg-blue-600 text-white rounded-md
                        hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
                        disabled:opacity-50 disabled:cursor-not-allowed transition-colors
                    "
                >
                    {loading ? "Logging in..." : "Login"}
                </button>
            </form>
        </div>
    </div>
</div>
