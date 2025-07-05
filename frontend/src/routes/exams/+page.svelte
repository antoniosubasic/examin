<script lang="ts">
    import { onMount } from "svelte";
    import { goto } from "$app/navigation";

    let sessionId = "";
    let schoolName = "";
    let loading = false;
    let searching = false;
    let error = "";
    let startDate = "";
    let endDate = "";
    let exams: Exam[] = [];

    onMount(() => {
        sessionId = localStorage.getItem("sessionId") || "";
        schoolName = localStorage.getItem("schoolName") || "";

        if (!sessionId) {
            goto("/");
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

    async function searchExams() {
        if (!startDate || !endDate) {
            error = "Please select both start and end dates";
            return;
        }

        if (new Date(startDate) > new Date(endDate)) {
            error = "Start date must be before end date";
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
                    setTimeout(() => {
                        localStorage.removeItem("sessionId");
                        localStorage.removeItem("schoolName");
                        goto("/");
                    }, 2000);
                    return;
                }
                throw new Error(
                    errorData.error || `HTTP error! status: ${response.status}`
                );
            }

            exams = await response.json();
        } catch (err) {
            error = `Failed to search exams: ${err instanceof Error ? err.message : "Unknown error"}`;
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

    function getExamTypeColor(type: string): string {
        switch (type?.toLowerCase()) {
            case "schularbeit":
            case "test":
                return "bg-red-100 text-red-800";
            default:
                return "bg-gray-100 text-gray-800";
        }
    }

    function exportToCsv() {
        if (exams.length === 0) {
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
                        class="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium"
                    >
                        Dashboard
                    </a>
                    <a
                        href="/exams"
                        class="bg-gray-100 text-gray-900 px-3 py-2 rounded-md text-sm font-medium"
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
            <div class="bg-white rounded-lg shadow p-6 mb-6">
                <h2 class="text-lg font-medium text-gray-900 mb-4">
                    Search Exams
                </h2>

                <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
                    <div>
                        <label
                            for="startDate"
                            class="block text-sm font-medium text-gray-700 mb-2"
                        >
                            Start Date
                        </label>
                        <input
                            id="startDate"
                            type="date"
                            bind:value={startDate}
                            class="
                                block w-full border border-gray-300 rounded-md px-3 py-2
                                focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                            "
                            disabled={searching}
                        />
                    </div>

                    <div>
                        <label
                            for="endDate"
                            class="block text-sm font-medium text-gray-700 mb-2"
                        >
                            End Date
                        </label>
                        <input
                            id="endDate"
                            type="date"
                            bind:value={endDate}
                            class="
                                block w-full border border-gray-300 rounded-md px-3 py-2
                                focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                            "
                            disabled={searching}
                        />
                    </div>

                    <div class="flex items-end">
                        <button
                            on:click={searchExams}
                            disabled={searching || !startDate || !endDate}
                            class="
                                w-full px-4 py-2 bg-blue-600 text-white rounded-md
                                hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
                                disabled:opacity-50 disabled:cursor-not-allowed
                            "
                        >
                            {searching ? "Searching..." : "Search Exams"}
                        </button>
                    </div>
                </div>

                {#if error}
                    <div
                        class="mt-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded"
                    >
                        {error}
                    </div>
                {/if}
            </div>

            {#if exams.length > 0}
                <div class="bg-white rounded-lg shadow">
                    <div class="px-6 py-4 border-b border-gray-200">
                        <div class="flex items-center justify-between">
                            <h3 class="text-lg font-medium text-gray-900">
                                Found {exams.length}
                                {exams.length === 1 ? "entry" : "entries"}
                            </h3>
                            <button
                                on:click={exportToCsv}
                                class="
                                    px-4 py-2 bg-green-600 text-white rounded-md text-sm font-medium
                                    hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2
                                    transition-colors duration-200
                                "
                                title="Export exams to CSV file"
                            >
                                Save as CSV
                            </button>
                        </div>
                    </div>

                    <div class="divide-y divide-gray-200">
                        {#each exams as exam}
                            <div class="px-6 py-4 hover:bg-gray-50">
                                <div class="flex items-center justify-between">
                                    <div class="flex-1">
                                        <div
                                            class="flex items-center space-x-3"
                                        >
                                            <h4
                                                class="text-lg font-medium text-gray-900"
                                            >
                                                {exam.name || "Unnamed Exam"}
                                            </h4>
                                            {#if exam.examType}
                                                <span
                                                    class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium {getExamTypeColor(
                                                        exam.examType
                                                    )}"
                                                >
                                                    {exam.examType}
                                                </span>
                                            {/if}
                                        </div>

                                        {#if exam.subject}
                                            <div class="mt-2">
                                                <span
                                                    class="inline-block text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1"
                                                >
                                                    Subject
                                                </span>
                                                <p
                                                    class="text-sm text-gray-800 font-medium"
                                                >
                                                    {exam.subject}
                                                </p>
                                            </div>
                                        {/if}

                                        {#if exam.text}
                                            <div class="mt-3">
                                                <span
                                                    class="inline-block text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1"
                                                >
                                                    Description
                                                </span>
                                                <p
                                                    class="text-sm text-gray-700 leading-relaxed"
                                                >
                                                    {exam.text}
                                                </p>
                                            </div>
                                        {/if}
                                    </div>

                                    <div class="ml-4 text-right">
                                        <p
                                            class="text-sm font-medium text-gray-900"
                                        >
                                            {formatDate(exam.examDate)}
                                        </p>
                                        <p class="text-sm text-gray-600">
                                            {formatTime(exam.startTime)} - {formatTime(
                                                exam.endTime
                                            )}
                                        </p>
                                    </div>
                                </div>
                            </div>
                        {/each}
                    </div>
                </div>
            {:else if exams.length === 0 && !searching && !error && startDate && endDate}
                <div class="bg-white rounded-lg shadow p-6 text-center">
                    <svg
                        class="mx-auto h-12 w-12 text-gray-400"
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
                    <h3 class="mt-2 text-sm font-medium text-gray-900">
                        No exams found
                    </h3>
                    <p class="mt-1 text-sm text-gray-500">
                        No exams were found for the selected date range.
                    </p>
                </div>
            {/if}
        </div>
    </main>
</div>
