<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { onMount } from "svelte";
    import { Button } from "$lib/components/ui/button";
    import {
        Card,
        CardContent,
        CardDescription,
        CardHeader,
        CardTitle,
    } from "$lib/components/ui/card";
    import { Input } from "$lib/components/ui/input";
    import { Label } from "$lib/components/ui/label";
    import { ChevronLeft } from "@lucide/svelte";
    import { authStore } from "$lib/stores/auth";
    import { toast } from "svelte-sonner";

    let schoolName = $state("");
    let schoolLoginName = $state("");
    let username = $state("");
    let password = $state("");
    let loading = $state(false);

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
            toast.error("Please enter both username and password");
            return;
        }

        loading = true;

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
                authStore.login(result.sessionId, schoolName);
                localStorage.setItem("justLoggedIn", "true");
                goto("/dashboard");
            } else {
                toast.error(
                    result.message ||
                        "Login failed. Please check your credentials."
                );
            }
        } catch (err) {
            toast.error("Login failed. Please try again.");
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

<div class="w-full flex items-center justify-center">
    <div class="space-y-4">
        <Button variant="ghost" onclick={goBack}>
            <ChevronLeft />
            Back to search
        </Button>

        <Card class="w-lg">
            <CardHeader>
                <CardTitle class="text-[1.75rem]">Login</CardTitle>
                <CardDescription class="text-base">
                    {schoolName}
                </CardDescription>
            </CardHeader>
            <CardContent>
                <form onsubmit={handleLogin} class="space-y-4">
                    <div class="space-y-2">
                        <Label for="username">Username</Label>
                        <Input
                            id="username"
                            type="text"
                            bind:value={username}
                            onkeypress={handleKeyPress}
                            placeholder="Enter your username"
                            disabled={loading}
                            required
                        />
                    </div>

                    <div class="space-y-2">
                        <Label for="password">Password</Label>
                        <Input
                            id="password"
                            type="password"
                            bind:value={password}
                            onkeypress={handleKeyPress}
                            placeholder="Enter your password"
                            disabled={loading}
                            required
                        />
                    </div>

                    <Button
                        type="submit"
                        disabled={loading ||
                            !username.trim() ||
                            !password.trim()}
                        class="w-full"
                    >
                        {loading ? "Logging in..." : "Login"}
                    </Button>
                </form>
            </CardContent>
        </Card>
    </div>
</div>
