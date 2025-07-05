<script lang="ts">
    import { onMount } from "svelte";
    import { goto } from "$app/navigation";

    let sessionId = "";
    let schoolName = "";
    let loading = false;
    let showFullSessionId = false;
    let showLoginAlert = false;

    onMount(() => {
        sessionId = localStorage.getItem("sessionId") || "";
        schoolName = localStorage.getItem("schoolName") || "";

        if (localStorage.getItem("justLoggedIn") === "true") {
            showLoginAlert = true;
            localStorage.removeItem("justLoggedIn");
        }

        if (!sessionId) {
            goto("/");
        }
    });

    async function handleLogout() {
        loading = true;

        try {
            await fetch("/api/auth/logout", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({ sessionId }),
            });
        } catch (err) {
            console.error("Logout error:", err);
        } finally {
            localStorage.removeItem("sessionId");
            localStorage.removeItem("schoolName");
            loading = false;
            goto("/");
        }
    }
</script>

<div class="min-h-screen bg-gray-50">
    <nav class="bg-white shadow-xs border-b border-gray-300">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div class="flex justify-between h-16">
                <nav class="flex items-center space-x-4">
                    <a
                        href="/dashboard"
                        class="bg-gray-100 text-gray-900 px-3 py-2 rounded-md text-sm font-medium"
                    >
                        Dashboard
                    </a>
                    <a
                        href="/exams"
                        class="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium"
                    >
                        Exams
                    </a>
                </nav>
                <div class="flex items-center space-x-4">
                    <span class="text-sm text-gray-600">{schoolName}</span>
                    <button
                        on:click={handleLogout}
                        disabled={loading}
                        class="
                            px-3 py-2 text-sm bg-red-600 text-white rounded-md
                            hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2
                            disabled:opacity-50
                        "
                    >
                        {loading ? "Logging out..." : "Logout"}
                    </button>
                </div>
            </div>
        </div>
    </nav>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div class="px-4 py-6 sm:px-0">
            <div class="bg-white rounded-lg shadow p-6">
                <h2 class="text-2xl font-bold text-gray-900 mb-4">
                    Welcome to your dashboard!
                </h2>

                {#if showLoginAlert}
                    <div
                        class="bg-green-50 border border-green-200 rounded-md p-4"
                    >
                        <div class="flex">
                            <div class="flex-shrink-0">
                                <svg
                                    class="h-5 w-5 text-green-400"
                                    fill="currentColor"
                                    viewBox="0 0 20 20"
                                >
                                    <path
                                        fill-rule="evenodd"
                                        d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                                        clip-rule="evenodd"
                                    />
                                </svg>
                            </div>
                            <div class="ml-3">
                                <h3 class="text-sm font-medium text-green-800">
                                    Login Successful
                                </h3>
                                <p class="text-sm text-green-700 mt-1">
                                    Your session is active. You can now access
                                    your data.
                                </p>
                            </div>
                        </div>
                    </div>
                {/if}

                <div class="mt-6">
                    <h3 class="text-lg font-medium text-gray-900 mb-3">
                        Quick Actions
                    </h3>
                    <div
                        class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3"
                    >
                        <a
                            href="/exams"
                            class="block p-6 bg-white border border-gray-200 rounded-lg shadow-sm hover:shadow-md hover:border-blue-300 transition-all duration-200"
                        >
                            <div class="flex items-center">
                                <div class="flex-shrink-0">
                                    <svg
                                        class="h-8 w-8 text-blue-600"
                                        fill="none"
                                        viewBox="0 0 24 24"
                                        stroke="currentColor"
                                    >
                                        <path
                                            stroke-linecap="round"
                                            stroke-linejoin="round"
                                            stroke-width="2"
                                            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                                        />
                                    </svg>
                                </div>
                                <div class="ml-4">
                                    <h4
                                        class="text-lg font-medium text-gray-900"
                                    >
                                        Search Exams
                                    </h4>
                                    <p class="text-sm text-gray-500">
                                        Find exams by date range
                                    </p>
                                </div>
                            </div>
                        </a>
                    </div>
                </div>

                <div class="mt-6">
                    <h3 class="text-lg font-medium text-gray-900 mb-3">
                        Session Information
                    </h3>
                    <dl class="grid grid-cols-1 gap-x-4 gap-y-2 sm:grid-cols-2">
                        <div>
                            <dt class="text-sm font-medium text-gray-500">
                                School
                            </dt>
                            <dd class="text-sm text-gray-900">{schoolName}</dd>
                        </div>
                        <div>
                            <dt class="text-sm font-medium text-gray-500">
                                Session ID
                            </dt>
                            <dd class="text-sm text-gray-900">
                                <button
                                    type="button"
                                    class="font-mono cursor-pointer hover:bg-gray-100 rounded transition-colors"
                                    on:click={() =>
                                        (showFullSessionId =
                                            !showFullSessionId)}
                                    title="Click to toggle full session ID"
                                >
                                    {showFullSessionId
                                        ? sessionId
                                        : `${sessionId.substring(0, 8)}...`}
                                </button>
                            </dd>
                        </div>
                    </dl>
                </div>
            </div>
        </div>
    </main>
</div>
