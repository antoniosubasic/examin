<script lang="ts">
    import { goto } from "$app/navigation";
    import { onMount } from "svelte";
    import { Button } from "$lib/components/ui/button";
    import { Card, CardContent } from "$lib/components/ui/card";
    import { Input } from "$lib/components/ui/input";
    import { Label } from "$lib/components/ui/label";
    import { toast } from "svelte-sonner";
    import { LightDarkToggle } from "$lib/components/light-dark-toggle";

    let searchQuery = "";
    let schools: School[] = [];
    let loading = false;

    onMount(() => {
        if (localStorage.getItem("sessionId") || "") {
            goto("/dashboard");
        }
    });

    async function searchSchools() {
        if (!searchQuery.trim()) {
            toast.error("Please enter a search query");
            return;
        }

        loading = true;
        schools = [];

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
                    toast.error("Too many results, please refine your search.");
                    return;
                }

                throw new Error(`HTTP error! status: ${response.status}`);
            }

            schools = await response.json();
            if (schools.length === 0) {
                toast.info("No schools found matching your query.");
            }
        } catch (err) {
            toast.error("Failed to search schools. Please try again.");
            console.error("Search error:", err);
        } finally {
            loading = false;
        }
    }
</script>

<div
    class="min-h-[100dvh] flex items-center justify-center px-4 py-12 relative"
>
    <div class="absolute top-4 right-4">
        <LightDarkToggle />
    </div>

    <div class="w-full max-w-md space-y-8">
        <div class="text-center">
            <h1 class="text-4xl font-bold tracking-tight">Find Your School</h1>
        </div>

        <Card>
            <CardContent class="space-y-4">
                <div class="space-y-2">
                    <Label for="search">School Name or Location</Label>
                    <div class="flex gap-2">
                        <Input
                            id="search"
                            type="text"
                            bind:value={searchQuery}
                            placeholder="Enter School Name or Location..."
                            disabled={loading}
                            class="flex-1"
                            onkeypress={(event) => {
                                if (event.key === "Enter") searchSchools();
                            }}
                        />
                        <Button
                            disabled={loading || !searchQuery.trim()}
                            class="px-6"
                            onclick={searchSchools}
                        >
                            {loading ? "Searching..." : "Search"}
                        </Button>
                    </div>
                </div>

                {#if schools.length > 0}
                    <div class="space-y-2 max-h-64 overflow-y-auto">
                        {#each schools as school}
                            <Button
                                variant="outline"
                                class="w-full justify-start h-auto p-4 text-left"
                                onclick={() =>
                                    goto(
                                        `/login?school=${encodeURIComponent(school.loginName)}&name=${encodeURIComponent(school.displayName)}`
                                    )}
                            >
                                <div class="flex flex-col items-start">
                                    <div class="font-medium">
                                        {school.displayName}
                                    </div>
                                    <div class="text-sm text-slate-500">
                                        {school.loginName}
                                    </div>
                                </div>
                            </Button>
                        {/each}
                    </div>
                {/if}
            </CardContent>
        </Card>
    </div>
</div>
