<script lang="ts">
    import { onMount } from "svelte";
    import { goto } from "$app/navigation";
    import { toast } from "svelte-sonner";
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
    import { Badge } from "$lib/components/ui/badge";
    import { Alert, AlertDescription } from "$lib/components/ui/alert";
    import {
        Table,
        TableBody,
        TableCell,
        TableHead,
        TableHeader,
        TableRow,
    } from "$lib/components/ui/table";
    import { Skeleton } from "$lib/components/ui/skeleton";
    import { Separator } from "$lib/components/ui/separator";
    import { FileTextIcon } from "@lucide/svelte";
    import { authStore } from "$lib/stores/auth";
    import { Navbar } from "$lib/components/navbar";

    let searching = false;
    let error = "";
    let startDate = "";
    let endDate = "";
    let exams: Exam[] = [];

    $: ({ sessionId, isAuthenticated } = $authStore);

    onMount(() => {
        authStore.init();

        if (!isAuthenticated) {
            goto("/");
            return;
        }

        const now = new Date();
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, "0");
        const firstDay = `${year}-${month}-01`;
        const lastDay = new Date(year, now.getMonth() + 1, 0).getDate();
        const lastDayFormatted = `${year}-${month}-${String(lastDay).padStart(2, "0")}`;

        startDate = firstDay;
        endDate = lastDayFormatted;
    });

    async function searchExams() {
        if (!startDate || !endDate) {
            error = "Please select both start and end dates";
            toast.error(error);
            return;
        }

        if (new Date(startDate) > new Date(endDate)) {
            error = "Start date must be before end date";
            toast.error(error);
            return;
        }

        searching = true;
        error = "";
        exams = [];

        try {
            const response = await fetch("/api/exams/range", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    sessionId: sessionId,
                    startDate: startDate,
                    endDate: endDate,
                }),
            });

            if (!response.ok) {
                const errorData = await response.json();
                if (response.status === 401) {
                    error = "Session expired. Please log in again.";
                    toast.error(error);
                    authStore.logout();
                    setTimeout(() => {
                        goto("/");
                    }, 2000);
                    return;
                }
                throw new Error(
                    errorData.error || `HTTP error! status: ${response.status}`
                );
            }

            exams = await response.json();
            toast.success(
                `Found ${exams.length} ${exams.length === 1 ? "exam" : "exams"}`
            );
        } catch (err) {
            error = `Failed to search exams: ${err instanceof Error ? err.message : "Unknown error"}`;
            toast.error(error);
            console.error("Search error:", err);
        } finally {
            searching = false;
        }
    }

    function formatDate(dateString: string): string {
        return new Date(dateString + "T00:00:00").toLocaleDateString();
    }

    function formatTime(timeString: string): string {
        return timeString.substring(0, 5);
    }

    function getExamTypeVariant(
        type: string
    ): "default" | "secondary" | "destructive" | "outline" {
        switch (type?.toLowerCase()) {
            case "schularbeit":
            case "test":
                return "destructive";
            default:
                return "secondary";
        }
    }

    function exportToCsv() {
        if (exams.length === 0) {
            toast.error("No exams to export");
            return;
        }

        const headers = "Subject,Description,Start Date,Start Time,End Time";
        const csvContent = [headers];

        exams.forEach((exam) => {
            const subject = exam.subject || "";
            const description = exam.text || exam.name || "";

            const examDate = new Date(exam.examDate + "T00:00:00");
            const formattedDate = examDate.toLocaleDateString("en-US");

            const formatTime = (timeStr: string) => {
                const parts = timeStr.split(":");
                const hours = parseInt(parts[0], 10);
                const minutes = parseInt(parts[1], 10);

                const date = new Date();
                date.setHours(hours, minutes, 0, 0);

                return date.toLocaleTimeString("en-US", {
                    hour: "numeric",
                    minute: "2-digit",
                    hour12: true,
                });
            };

            const startTime = formatTime(exam.startTime);
            const endTime = formatTime(exam.endTime);

            const escapeCsvValue = (value: string) => {
                if (
                    value.includes(",") ||
                    value.includes('"') ||
                    value.includes("\n")
                ) {
                    return `"${value.replace(/"/g, '""')}"`;
                }
                return value;
            };

            const row = [
                escapeCsvValue(subject),
                escapeCsvValue(description),
                formattedDate,
                startTime,
                endTime,
            ].join(",");

            csvContent.push(row);
        });

        const csvString = csvContent.join("\n");
        const blob = new Blob([csvString], { type: "text/csv;charset=utf-8;" });
        const link = document.createElement("a");

        if (link.download !== undefined) {
            const url = URL.createObjectURL(blob);
            link.setAttribute("href", url);
            link.setAttribute("download", "events.csv");
            link.style.visibility = "hidden";
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            toast.success("Successfully exported to CSV");
        }
    }
</script>

<div class="min-h-screen bg-background">
    <Navbar />

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div class="px-4 py-6 sm:px-0 space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle>Search Exams</CardTitle>
                    <CardDescription>
                        Find exams within a specific date range
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
                        <div class="space-y-2">
                            <Label for="startDate">Start Date</Label>
                            <Input
                                id="startDate"
                                type="date"
                                bind:value={startDate}
                                disabled={searching}
                            />
                        </div>

                        <div class="space-y-2">
                            <Label for="endDate">End Date</Label>
                            <Input
                                id="endDate"
                                type="date"
                                bind:value={endDate}
                                disabled={searching}
                            />
                        </div>

                        <div class="flex items-end">
                            <Button
                                class="w-full"
                                onclick={searchExams}
                                disabled={searching || !startDate || !endDate}
                            >
                                {searching ? "Searching..." : "Search Exams"}
                            </Button>
                        </div>
                    </div>

                    {#if error}
                        <Alert variant="destructive" class="mt-4">
                            <AlertDescription>{error}</AlertDescription>
                        </Alert>
                    {/if}
                </CardContent>
            </Card>

            {#if searching}
                <Card>
                    <CardHeader>
                        <CardTitle>
                            <Skeleton class="h-6 w-32" />
                        </CardTitle>
                    </CardHeader>
                    <CardContent class="space-y-4">
                        {#each Array(3) as _}
                            <div class="space-y-2">
                                <Skeleton class="h-4 w-full" />
                                <Skeleton class="h-4 w-3/4" />
                                <Skeleton class="h-4 w-1/2" />
                            </div>
                            <Separator />
                        {/each}
                    </CardContent>
                </Card>
            {:else if exams.length > 0}
                <Card>
                    <CardHeader>
                        <div class="flex items-center justify-between">
                            <CardTitle class="text-xl">
                                Found {exams.length}
                                {exams.length === 1 ? "exam" : "exams"}
                            </CardTitle>
                            <Button onclick={exportToCsv}>Export CSV</Button>
                        </div>
                    </CardHeader>
                    <CardContent>
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead class="text-lg">Exam</TableHead>
                                    <TableHead class="text-lg">Subject</TableHead>
                                    <TableHead class="text-lg">Date</TableHead>
                                    <TableHead class="text-lg">Time</TableHead>
                                    <TableHead class="text-lg">Description</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {#each exams as exam}
                                    <TableRow>
                                        <TableCell>
                                            <div class="flex gap-3">
                                                <span class="font-medium">
                                                    {exam.name ||
                                                        "Unnamed Exam"}
                                                </span>
                                                {#if exam.examType}
                                                    <Badge
                                                        variant={getExamTypeVariant(
                                                            exam.examType
                                                        )}
                                                    >
                                                        {exam.examType}
                                                    </Badge>
                                                {/if}
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <span
                                                class="font-medium text-foreground"
                                            >
                                                {exam.subject || "—"}
                                            </span>
                                        </TableCell>
                                        <TableCell>
                                            <span class="text-sm">
                                                {formatDate(exam.examDate)}
                                            </span>
                                        </TableCell>
                                        <TableCell>
                                            <div
                                                class="text-sm flex flex-col gap-1"
                                            >
                                                <p>
                                                    {formatTime(exam.startTime)}
                                                </p>
                                                <p>
                                                    {formatTime(exam.endTime)}
                                                </p>
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <span
                                                class="text-sm text-muted-foreground"
                                            >
                                                {exam.text || "—"}
                                            </span>
                                        </TableCell>
                                    </TableRow>
                                {/each}
                            </TableBody>
                        </Table>
                    </CardContent>
                </Card>
            {:else if !searching && !error && startDate && endDate}
                <Card>
                    <CardContent class="text-center py-12">
                        <FileTextIcon
                            class="mx-auto h-12 w-12 text-muted-foreground mb-4"
                        />
                        <CardTitle class="text-muted-foreground"
                            >No exams found</CardTitle
                        >
                        <CardDescription class="mt-2">
                            No exams were found for the selected date range.
                        </CardDescription>
                    </CardContent>
                </Card>
            {/if}
        </div>
    </main>
</div>
