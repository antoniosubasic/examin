<script lang="ts">
    import { goto } from "$app/navigation";
    import { onMount } from "svelte";

    let searchQuery = "";
    let schools: School[] = [];
    let loading = false;
    let error = "";

    onMount(() => {
        if (localStorage.getItem("sessionId") || "") {
            goto("/dashboard");
        }
    });

    async function searchSchools() {
        if (!searchQuery.trim()) {
            error = "Please enter a search query";
            return;
        }

        loading = true;
        schools = [];
        error = "";

        try {
            const response = await fetch(
                `/api/schools/search?query=${encodeURIComponent(searchQuery)}`
            );

            if (!response.ok) {
                if (
                    await response
                        .json()
                        .then((res) => res.error === "too many results")
                ) {
                    error = "Too many results, please refine your search.";
                    return;
                }

                throw new Error(`HTTP error! status: ${response.status}`);
            }

            schools = await response.json();
        } catch (err) {
            error = "Failed to search schools. Please try again.";
            console.error("Search error:", err);
        } finally {
            loading = false;
        }
    }

    function selectSchool(school: School) {
        goto(
            `/login?school=${encodeURIComponent(school.loginName)}&name=${encodeURIComponent(school.displayName)}`
        );
    }

    function handleKeyPress(event: KeyboardEvent) {
        if (event.key === "Enter") {
            searchSchools();
        }
    }
</script>

<div
    class="min-h-screen flex items-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8"
>
    <div class="max-w-md mx-auto">
        <div class="text-center mb-8">
            <h1 class="text-3xl font-bold text-gray-900 mb-2">Search School</h1>
            <p class="text-gray-600">Search for your school to get started</p>
        </div>

        <div class="min-w-md bg-white rounded-lg shadow-md p-6">
            <div class="mb-4">
                <label
                    for="search"
                    class="block text-sm font-medium text-gray-700 mb-2"
                >
                    School Name or Location
                </label>
                <div class="flex gap-2">
                    <input
                        id="search"
                        type="text"
                        bind:value={searchQuery}
                        on:keypress={handleKeyPress}
                        placeholder="school name or location..."
                        class="
                            flex-1 border border-gray-300 rounded-md px-3 py-2
                            focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                        "
                        disabled={loading}
                    />
                    <button
                        on:click={searchSchools}
                        disabled={loading || !searchQuery.trim()}
                        class="
                            min-w-30 px-4 py-2 bg-blue-600 text-white rounded-md
                            hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
                            disabled:opacity-50 disabled:cursor-not-allowed
                        "
                    >
                        {loading ? "Searching..." : "Search"}
                    </button>
                </div>
            </div>

            {#if error}
                <div
                    class="mb-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded"
                >
                    {error}
                </div>
            {/if}

            {#if schools.length > 0}
                <div class="space-y-2">
                    {#each schools as school}
                        <button
                            on:click={() => selectSchool(school)}
                            class="
                                w-full text-left p-3 border border-gray-200 rounded-md hover:bg-gray-50 hover:border-gray-300
                                focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-colors
                            "
                        >
                            <div class="font-medium text-gray-900">
                                {school.displayName}
                            </div>
                            <div class="text-sm text-gray-500">
                                {school.loginName}
                            </div>
                        </button>
                    {/each}
                </div>
            {/if}
        </div>
    </div>
</div>
