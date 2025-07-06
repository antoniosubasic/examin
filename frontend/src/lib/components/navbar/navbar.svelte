<script lang="ts">
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
    import { Button } from "$lib/components/ui/button";
    import {
        NavigationMenuRoot,
        NavigationMenuItem,
        NavigationMenuLink,
        NavigationMenuList,
    } from "$lib/components/ui/navigation-menu";
    import { toast } from "svelte-sonner";
    import { toggleMode } from "mode-watcher";
    import { School, LogOut, MoonIcon, SunIcon } from "@lucide/svelte";
    import { authStore } from "$lib/stores/auth";

    let loading = false;

    $: ({ sessionId } = $authStore);
    $: currentPath = page.url.pathname;

    function getLinkClasses(path: string): string {
        const baseClasses = `
            group inline-flex h-9 w-max items-center justify-center
            rounded-md bg-background px-4 py-2 text-sm font-medium transition-colors
            hover:bg-accent hover:text-accent-foreground
            focus:bg-accent focus:text-accent-foreground focus:outline-none
            disabled:pointer-events-none disabled:opacity-50
            data-[active]:bg-accent/50 data-[state=open]:bg-accent/50
        `;

        return `${baseClasses} ${
            currentPath.startsWith(path) ? "" : "text-muted-foreground"
        }`;
    }

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
            toast.success("Logged out successfully");
        } catch (err) {
            console.error("Logout error:", err);
            toast.error("Failed to logout");
        } finally {
            authStore.logout();
            loading = false;
            goto("/");
        }
    }
</script>

<header class="border-b">
    <div class="container mx-auto px-4 lg:px-8">
        <div class="flex h-16 items-center justify-between">
            <div class="flex items-center space-x-6">
                <a
                    class="flex items-center space-x-2 px-2 py-4 hover:cursor-pointer"
                    href="/"
                >
                    <School class="h-6 w-6 text-primary" />
                    <span class="text-xl font-semibold">Examin</span>
                </a>
                <NavigationMenuRoot class="hidden md:flex">
                    <NavigationMenuList>
                        {#each ["/dashboard", "/exams"] as link}
                            <NavigationMenuItem>
                                <NavigationMenuLink
                                    href={link}
                                    class={getLinkClasses(link)}
                                >
                                    {`${link.substring(1).charAt(0).toUpperCase()}${link.substring(2)}`}
                                </NavigationMenuLink>
                            </NavigationMenuItem>
                        {/each}
                    </NavigationMenuList>
                </NavigationMenuRoot>
            </div>

            <div>
                <Button onclick={toggleMode} variant="outline" size="icon">
                    <SunIcon
                        class="h-[1.2rem] w-[1.2rem] rotate-0 scale-100 !transition-all dark:-rotate-90 dark:scale-0"
                    />
                    <MoonIcon
                        class="absolute h-[1.2rem] w-[1.2rem] rotate-90 scale-0 !transition-all dark:rotate-0 dark:scale-100"
                    />
                    <span class="sr-only">Toggle theme</span>
                </Button>
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
    </div>
</header>
