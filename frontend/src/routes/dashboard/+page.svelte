<script lang="ts">
    import { onMount } from "svelte";
    import { goto } from "$app/navigation";
    import { Button } from "$lib/components/ui/button";
    import {
        Card,
        CardContent,
        CardDescription,
        CardHeader,
        CardTitle,
    } from "$lib/components/ui/card";
    import {
        Alert,
        AlertDescription,
        AlertTitle,
    } from "$lib/components/ui/alert";
    import { Separator } from "$lib/components/ui/separator";
    import { Label } from "$lib/components/ui/label";
    import {
        NavigationMenuRoot,
        NavigationMenuItem,
        NavigationMenuLink,
        NavigationMenuList,
    } from "$lib/components/ui/navigation-menu";
    import { BookOpen, CheckCircle2Icon, School, LogOut } from "@lucide/svelte";

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

<div class="min-h-screen">
    <header class="border-b">
        <div class="container mx-auto px-4 lg:px-8">
            <div class="flex h-16 items-center justify-between">
                <div class="flex items-center space-x-6">
                    <div class="flex items-center space-x-2">
                        <School class="h-6 w-6 text-primary" />
                        <span class="text-xl font-semibold">Examin</span>
                    </div>
                    <NavigationMenuRoot class="hidden md:flex">
                        <NavigationMenuList>
                            <NavigationMenuItem>
                                <NavigationMenuLink
                                    href="/dashboard"
                                    class="
                                        group inline-flex h-9 w-max items-center justify-center
                                        rounded-md bg-background px-4 py-2 text-sm font-medium transition-colors
                                        hover:bg-accent hover:text-accent-foreground
                                        focus:bg-accent focus:text-accent-foreground focus:outline-none
                                        disabled:pointer-events-none disabled:opacity-50
                                        data-[active]:bg-accent/50 data-[state=open]:bg-accent/50
                                    "
                                >
                                    Dashboard
                                </NavigationMenuLink>
                            </NavigationMenuItem>
                            <NavigationMenuItem>
                                <NavigationMenuLink
                                    href="/exams"
                                    class="
                                        group inline-flex h-9 w-max items-center justify-center
                                        rounded-md bg-background px-4 py-2 text-sm font-medium transition-colors
                                        hover:bg-accent hover:text-accent-foreground
                                        focus:bg-accent focus:text-accent-foreground focus:outline-none
                                        disabled:pointer-events-none disabled:opacity-50
                                        data-[active]:bg-accent/50 data-[state=open]:bg-accent/50
                                        text-muted-foreground
                                    "
                                >
                                    Exams
                                </NavigationMenuLink>
                            </NavigationMenuItem>
                        </NavigationMenuList>
                    </NavigationMenuRoot>
                </div>

                <Button
                    variant="outline"
                    size="sm"
                    onclick={handleLogout}
                    disabled={loading}
                    class="ml-2"
                >
                    <LogOut class="h-4 w-4 mr-1" />
                    {loading ? "Logging out..." : "Logout"}
                </Button>
            </div>
        </div>
    </header>

    <main class="container mx-auto px-4 py-8 lg:px-8">
        <div class="space-y-8">
            <div class="space-y-2">
                <h1 class="text-3xl font-bold tracking-tight">Welcome back!</h1>
                <p class="text-muted-foreground">
                    Manage your exams and access your information.
                </p>
            </div>

            {#if showLoginAlert}
                <Alert class="border-green-200 bg-green-50">
                    <CheckCircle2Icon color="#016630" />
                    <AlertTitle class="text-green-800"
                        >Login Successful</AlertTitle
                    >
                    <AlertDescription class="text-green-700">
                        Your session is active. You can now access your data.
                    </AlertDescription>
                </Alert>
            {/if}

            <section class="space-y-4">
                <h2 class="text-2xl font-semibold tracking-tight">
                    Quick Actions
                </h2>

                <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                    <Card
                        class="group cursor-pointer transition-all hover:shadow-md hover:border-primary/20"
                    >
                        <CardContent>
                            <div class="flex items-center space-x-2 gap-2">
                                <div
                                    class="rounded-lg bg-blue-100 p-2 dark:bg-blue-900/20"
                                >
                                    <BookOpen
                                        class="h-5 w-5 text-blue-600 dark:text-blue-400"
                                    />
                                </div>
                                <div>
                                    <CardTitle class="text-lg">
                                        Search Exams
                                    </CardTitle>
                                    <CardDescription>
                                        Find exams by date range
                                    </CardDescription>
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </section>

            <Separator />

            <section class="space-y-4">
                <Card>
                    <CardHeader>
                        <CardTitle class="text-2xl">
                            Session Information
                        </CardTitle>
                    </CardHeader>
                    <CardContent class="space-y-4">
                        <div class="grid gap-4 md:grid-cols-2">
                            <div class="flex flex-col gap-2">
                                <Label
                                    class="text-sm font-medium text-muted-foreground"
                                >
                                    School
                                </Label>
                                <div class="flex items-center space-x-2">
                                    <School
                                        class="h-4 w-4 text-muted-foreground"
                                    />
                                    <span class="text-sm font-medium"
                                        >{schoolName}</span
                                    >
                                </div>
                            </div>

                            <div class="flex flex-col gap-2">
                                <Label
                                    class="text-sm font-medium text-muted-foreground"
                                >
                                    Session ID
                                </Label>
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    class="h-auto p-0 font-mono text-left justify-start"
                                    onclick={() =>
                                        (showFullSessionId =
                                            !showFullSessionId)}
                                >
                                    {showFullSessionId
                                        ? sessionId
                                        : `${sessionId.substring(0, 8)}...`}
                                </Button>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </section>
        </div>
    </main>
</div>
