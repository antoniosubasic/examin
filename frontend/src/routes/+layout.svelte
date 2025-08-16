<script lang="ts">
    import "../app.css";
    import { Toaster } from "$lib/components/ui/sonner";
    import { ModeWatcher } from "mode-watcher";
    import { Navbar } from "@/components/navbar";

    let { children } = $props();
</script>

<div id="root" class="min-h-[100dvh] flex flex-col">
    <header>
        <Navbar />
    </header>
    <main class="flex flex-1 justify-center px-4 py-12">
        {@render children()}
    </main>
</div>
<ModeWatcher />
<Toaster />
