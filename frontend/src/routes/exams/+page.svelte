<script lang="ts">
    import { onMount } from "svelte";
    import { goto } from "$app/navigation";
    import { toast } from "svelte-sonner";
    import { Button, buttonVariants } from "$lib/components/ui/button";
    import {
        Card,
        CardContent,
        CardDescription,
        CardHeader,
        CardTitle,
    } from "$lib/components/ui/card";
    import { Badge } from "$lib/components/ui/badge";
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
    import { RangeCalendar } from "$lib/components/ui/range-calendar";
    import {
        DateFormatter,
        getLocalTimeZone,
        parseDate,
        parseTime,
        today,
    } from "@internationalized/date";
    import type { DateRange } from "bits-ui";
    import { cn } from "$lib/utils";
    import {
        Popover,
        PopoverTrigger,
        PopoverContent,
    } from "$lib/components/ui/popover";
    import { FileTextIcon, CalendarIcon } from "@lucide/svelte";
    import { authStore } from "$lib/stores/auth";

    let searching = false;
    let dateRange: DateRange = {
        start: today(getLocalTimeZone()),
        end: today(getLocalTimeZone()).add({ months: 1 }),
    };
    let exams: Exam[] = [];

    const userLocale =
        navigator.language || navigator.languages?.[0] || "en-US";
    const df = new DateFormatter(userLocale, {
        dateStyle: "medium",
    });
    const tf = new DateFormatter(userLocale, {
        timeStyle: "short",
        hour12: false,
    });

    const timeToDate = (timeStr: string) => {
        const time = parseTime(timeStr);
        const date = new Date();
        date.setHours(time.hour, time.minute, time.second || 0, 0);
        return date;
    };

    onMount(() => {
        authStore.init();

        if (!$authStore.isAuthenticated) {
            goto("/");
            return;
        }
    });

    async function searchExams() {
        if (!dateRange.start || !dateRange.end) {
            toast.error("Please select both start and end dates");
            return;
        }

        if (dateRange.start > dateRange.end) {
            toast.error("Start date must be before end date");
            return;
        }

        searching = true;
        exams = [];

        try {
            const response = await fetch("/api/exams/range", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    sessionId: $authStore.sessionId,
                    startDate: dateRange.start.toString(),
                    endDate: dateRange.end.toString(),
                }),
            });

            if (!response.ok) {
                const errorData = await response.json();
                if (response.status === 401) {
                    toast.error("Session expired. Please log in again.");
                    setTimeout(() => {
                        authStore.logout();
                        goto("/");
                    }, 2000);
                    return;
                }
                throw new Error(
                    errorData.error || `HTTP error! status: ${response.status}`
                );
            }

            exams = await response.json();
            const message = `Found ${exams.length} ${exams.length === 1 ? "exam" : "exams"}`;
            exams.length === 0 ? toast.info(message) : toast.success(message);
        } catch (err) {
            toast.error(
                `Failed to search exams: ${err instanceof Error ? err.message : "Unknown error"}`
            );
            console.error("Search error:", err);
        } finally {
            searching = false;
        }
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

        const df = new DateFormatter("en-US", {
            day: "numeric",
            month: "numeric",
            year: "numeric",
        });

        const tf = new DateFormatter("en-US", {
            hour: "numeric",
            minute: "2-digit",
            hour12: true,
        });

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

        const csvContent = [
            "Subject,Description,Start Date,Start Time,End Time",
        ];
        csvContent.push(
            ...exams.map((exam) =>
                [
                    escapeCsvValue(exam.subject || ""),
                    escapeCsvValue(exam.text || exam.name || ""),
                    df.format(
                        parseDate(exam.examDate).toDate(getLocalTimeZone())
                    ),
                    tf.format(timeToDate(exam.startTime)),
                    tf.format(timeToDate(exam.endTime)),
                ].join(",")
            )
        );

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

<div class="w-full md:mx-[4vw] xl:mx-[10vw] flex flex-col items-center gap-10">
    <div class="flex flex-col justify-content gap-4">
        <Popover>
            <PopoverTrigger class={cn(buttonVariants({ variant: "outline" }))}>
                <CalendarIcon class="mr-2 size-4" />
                {#if dateRange && dateRange.start}
                    {df.format(dateRange.start.toDate(getLocalTimeZone()))}
                    {#if dateRange.end}
                        - {df.format(dateRange.end.toDate(getLocalTimeZone()))}
                    {/if}
                {:else}
                    Pick a date
                {/if}
            </PopoverTrigger>
            <PopoverContent class="w-auto p-0">
                <RangeCalendar bind:value={dateRange} disabled={searching} />
            </PopoverContent>
        </Popover>

        <div class="flex items-end">
            <Button
                class="w-full"
                onclick={searchExams}
                disabled={searching || !dateRange.start || !dateRange.end}
            >
                {searching ? "Searching..." : "Search Exams"}
            </Button>
        </div>
    </div>

    <Card class="w-full">
        {#if searching}
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
        {:else if exams.length > 0}
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
                                            {exam.name || "Unnamed Exam"}
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
                                    <span class="font-medium text-foreground">
                                        {exam.subject || "—"}
                                    </span>
                                </TableCell>
                                <TableCell>
                                    <span class="text-sm">
                                        {df.format(
                                            parseDate(exam.examDate).toDate(
                                                getLocalTimeZone()
                                            )
                                        )}
                                    </span>
                                </TableCell>
                                <TableCell>
                                    <div class="text-sm flex flex-col gap-1">
                                        <p>
                                            {tf.format(
                                                timeToDate(exam.startTime)
                                            )}
                                        </p>
                                        <p>
                                            {tf.format(
                                                timeToDate(exam.endTime)
                                            )}
                                        </p>
                                    </div>
                                </TableCell>
                                <TableCell>
                                    <span class="text-sm text-muted-foreground">
                                        {exam.text || "—"}
                                    </span>
                                </TableCell>
                            </TableRow>
                        {/each}
                    </TableBody>
                </Table>
            </CardContent>
        {:else if !searching && dateRange.start && dateRange.end}
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
        {/if}
    </Card>
</div>
