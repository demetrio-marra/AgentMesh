(() => {
    const chatIdKey = "agentmesh-chat-id";
    const chatId = sessionStorage.getItem(chatIdKey) || crypto.randomUUID();
    sessionStorage.setItem(chatIdKey, chatId);

    const transcript = document.getElementById("transcript");
    const composer = document.getElementById("composer");
    const message = document.getElementById("message");
    const send = document.getElementById("send");
    const stop = document.getElementById("stop");
    const newChat = document.getElementById("new-chat");
    const progress = document.getElementById("progress");
    const error = document.getElementById("error");
    const tokenCount = document.getElementById("token-count");
    const costCount = document.getElementById("cost-count");
    const configurationRail = document.getElementById("configuration-rail");
    const configurationToggle = document.getElementById("configuration-toggle");
    const configurationClose = document.getElementById("configuration-close");
    const diagnosticsModal = document.getElementById("diagnostics-modal");
    const diagnosticsOutput = document.getElementById("diagnostics-output");
    const diagnosticsToggle = document.getElementById("diagnostics-toggle");
    const diagnosticsClose = document.getElementById("diagnostics-close");
    const diagnosticsSummaryTab = document.getElementById("diagnostics-summary-tab");
    const diagnosticsStepsTab = document.getElementById("diagnostics-steps-tab");
    const diagnosticsRawTab = document.getElementById("diagnostics-raw-tab");
    const diagnosticsSummaryPanel = document.getElementById("diagnostics-summary-panel");
    const diagnosticsStepsPanel = document.getElementById("diagnostics-steps-panel");
    const diagnosticsRawPanel = document.getElementById("diagnostics-raw-panel");
    const diagnosticsSummary = document.getElementById("diagnostics-summary");
    const diagnosticsStepList = document.getElementById("diagnostics-step-list");
    const diagnosticsStepDetails = document.getElementById("diagnostics-step-details");
    const discardWarning = "The conversation will be lost. Continue?";
    const diagnosticsEvents = [];
    let isActive = false;
    let hasConversationContent = false;
    let selectedDiagnosticsStep = -1;
    let workflowSummary = null;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();

    function scrollTranscriptToBottom() {
        transcript.scrollTop = transcript.scrollHeight;
    }

    function createCopyButton(text) {
        const button = document.createElement("button");
        button.className = "message-copy";
        button.type = "button";
        button.title = "Copy message";
        button.setAttribute("aria-label", "Copy message");
        button.textContent = "Copy";
        button.dataset.copyText = text;
        return button;
    }

    function renderMessages(messages) {
        hasConversationContent = messages.length > 0;
        transcript.replaceChildren();
        if (!messages.length) {
            const empty = document.createElement("div");
            empty.className = "empty-state";
            empty.textContent = "Conversation is empty";
            transcript.append(empty);
            scrollTranscriptToBottom();
            return;
        }

        for (const messageItem of messages) {
            const article = document.createElement("article");
            article.className = `message message-${messageItem.role.toLowerCase()}`;
            const role = document.createElement("div");
            role.className = "message-role";
            role.textContent = messageItem.role;
            const body = document.createElement("div");
            body.className = "message-body";
            body.innerHTML = DOMPurify.sanitize(marked.parse(messageItem.text));
            const actions = document.createElement("div");
            actions.className = "message-actions";
            actions.append(createCopyButton(messageItem.text));
            article.append(role, body, actions);
            transcript.append(article);
        }
        scrollTranscriptToBottom();
    }

    function renderPending(text) {
        const empty = transcript.querySelector(".empty-state");
        if (empty) empty.remove();
        const pending = document.createElement("article");
        pending.className = "message message-user pending";
        pending.dataset.pending = "true";
        const role = document.createElement("div");
        role.className = "message-role";
        role.textContent = "User";
        const body = document.createElement("div");
        body.className = "message-body";
        body.textContent = text;
        const actions = document.createElement("div");
        actions.className = "message-actions";
        actions.append(createCopyButton(text));
        pending.append(role, body, actions);
        transcript.append(pending);
        hasConversationContent = true;
        scrollTranscriptToBottom();
    }

    function setOperation(state) {
        isActive = state === "chat" || state === "summarizing";
        message.disabled = isActive;
        send.hidden = isActive;
        stop.hidden = !isActive;
        send.disabled = isActive || !message.value.trim();
        if (state === "canceled") progress.textContent = "Request canceled.";
        if (state === "failed") progress.textContent = "Request failed.";
        if (state === "idle") progress.textContent = "";
    }

    function showError(text) {
        error.textContent = text;
        error.hidden = !text;
    }

    function setConfigurationVisible(isVisible) {
        configurationRail.classList.toggle("is-open", isVisible);
        configurationToggle.setAttribute("aria-expanded", String(isVisible));
    }

    function setDiagnosticsVisible(isVisible) {
        diagnosticsModal.hidden = !isVisible;
        diagnosticsToggle.setAttribute("aria-expanded", String(isVisible));
        if (isVisible) {
            setDiagnosticsTab("summary");
            diagnosticsSummaryTab.focus();
        }
    }

    function setDiagnosticsTab(tab) {
        const showSummary = tab === "summary";
        const showSteps = tab === "steps";
        diagnosticsSummaryTab.classList.toggle("is-selected", showSummary);
        diagnosticsStepsTab.classList.toggle("is-selected", showSteps);
        diagnosticsRawTab.classList.toggle("is-selected", !showSummary && !showSteps);
        diagnosticsSummaryTab.setAttribute("aria-selected", String(showSummary));
        diagnosticsStepsTab.setAttribute("aria-selected", String(showSteps));
        diagnosticsRawTab.setAttribute("aria-selected", String(!showSummary && !showSteps));
        diagnosticsSummaryTab.tabIndex = showSummary ? 0 : -1;
        diagnosticsStepsTab.tabIndex = showSteps ? 0 : -1;
        diagnosticsRawTab.tabIndex = !showSummary && !showSteps ? 0 : -1;
        diagnosticsSummaryPanel.hidden = !showSummary;
        diagnosticsStepsPanel.hidden = !showSteps;
        diagnosticsRawPanel.hidden = showSummary || showSteps;
        if (!showSummary && !showSteps) diagnosticsOutput.focus();
    }

    function readPayload(rawData) {
        try {
            return JSON.parse(rawData);
        } catch {
            return {};
        }
    }

    function parameterRows(parameters) {
        return Array.isArray(parameters)
            ? parameters.filter(parameter => parameter && typeof parameter === "object" && parameter.name != null).map(parameter => ({
                name: String(parameter.name),
                value: parameter.value == null ? "" : String(parameter.value)
            }))
            : [];
    }

    function formatElapsed(value) {
        if (typeof value !== "string") return "";
        const match = value.match(/^(\d+):(\d{2}):(\d{2})(?:\.(\d+))?$/);
        if (!match) return "";
        const hours = Number(match[1]);
        const minutes = Number(match[2]);
        const seconds = Number(match[3]);
        const fraction = Number(`0.${match[4] || "0"}`);
        if (hours === 0 && minutes === 0 && seconds === 0 && fraction < 1) return "<1s";
        return [hours ? `${hours}h` : "", minutes ? `${minutes}m` : "", seconds || fraction ? `${seconds}s` : ""].filter(Boolean).join(" ") || "<1s";
    }

    function formatDurationMilliseconds(milliseconds) {
        if (!Number.isFinite(milliseconds) || milliseconds < 1000) return "<1s";
        let remaining = Math.floor(milliseconds / 1000);
        const hours = Math.floor(remaining / 3600);
        remaining %= 3600;
        const minutes = Math.floor(remaining / 60);
        const seconds = remaining % 60;
        return [hours ? `${hours}h` : "", minutes ? `${minutes}m` : "", seconds ? `${seconds}s` : ""].filter(Boolean).join(" ");
    }

    function summaryElapsed(step) {
        const started = Date.parse(step.startedOnUtc);
        const completed = Date.parse(step.completedOnUtc);
        return formatDurationMilliseconds(completed - started);
    }

    function summaryCell(row, value, tagName = "td") {
        const cell = document.createElement(tagName);
        cell.textContent = value;
        row.append(cell);
    }

    function createSummaryTable(title, headers, rows, totalValues) {
        const wrapper = document.createElement("div");
        wrapper.className = "diagnostics-summary-table-wrap";
        const table = document.createElement("table");
        table.className = "diagnostics-summary-table";
        const caption = document.createElement("caption");
        caption.textContent = title;
        table.append(caption);
        const head = document.createElement("thead");
        const headerRow = document.createElement("tr");
        headers.forEach(header => summaryCell(headerRow, header, "th"));
        head.append(headerRow);
        table.append(head);
        const body = document.createElement("tbody");
        rows.forEach(values => {
            const row = document.createElement("tr");
            values.forEach(value => summaryCell(row, value));
            body.append(row);
        });
        table.append(body);
        const foot = document.createElement("tfoot");
        const totalRow = document.createElement("tr");
        totalValues.forEach(value => summaryCell(totalRow, value));
        foot.append(totalRow);
        table.append(foot);
        wrapper.append(table);
        return wrapper;
    }

    function renderDiagnosticsSummary() {
        diagnosticsSummary.replaceChildren();
        if (!workflowSummary || !Array.isArray(workflowSummary.mainPipelineStepsData) || !workflowSummary.mainPipelineStepsData.length) {
            const empty = document.createElement("p");
            empty.className = "diagnostics-summary-empty";
            empty.textContent = "No completed workflow summary is available.";
            diagnosticsSummary.append(empty);
            return;
        }

        const costsByAgent = new Map();
        for (const cost of workflowSummary.agentsCostData || []) {
            const queue = costsByAgent.get(cost.agentName) || [];
            queue.push(cost);
            costsByAgent.set(cost.agentName, queue);
        }
        const tokenRows = [];
        const hourlyRows = [];
        for (const step of workflowSummary.mainPipelineStepsData) {
            if (!step.isAgentic) {
                tokenRows.push({ step, cost: null });
                continue;
            }
            const queue = costsByAgent.get(step.agentName || "");
            const cost = queue && queue.length ? queue.shift() : null;
            if (cost && cost.costPerHour != null) hourlyRows.push({ step, cost });
            else tokenRows.push({ step, cost });
        }

        const tokenAgenticRows = tokenRows.filter(row => row.step.isAgentic);
        const totalInputTokens = tokenAgenticRows.reduce((total, row) => total + (row.step.inputTokens || 0), 0);
        const totalOutputTokens = tokenAgenticRows.reduce((total, row) => total + (row.step.outputTokens || 0), 0);
        const inputCost = row => row.cost ? row.cost.consumedInputTokens / 1000000 * row.cost.costPerMillionInputTokens : 0;
        const outputCost = row => row.cost ? row.cost.consumedOutputTokens / 1000000 * row.cost.costPerMillionOutputTokens : 0;
        const totalInputCost = tokenAgenticRows.reduce((total, row) => total + inputCost(row), 0);
        const totalOutputCost = tokenAgenticRows.reduce((total, row) => total + outputCost(row), 0);
        const percentage = (value, total) => total ? `${(value * 100 / total).toFixed(2)}%` : "0.00%";
        const money = value => `$${Number(value).toFixed(6)}`;
        const tokenTableRows = tokenRows.map(row => {
            const step = row.step;
            if (!step.isAgentic) return [step.stepName, summaryElapsed(step), "Unavailable", "Unavailable", "Unavailable", "Unavailable", "Unavailable", "Unavailable", "Unavailable"];
            const input = step.inputTokens || 0;
            const output = step.outputTokens || 0;
            return [step.stepName, summaryElapsed(step), input.toLocaleString(), percentage(input, totalInputTokens), money(inputCost(row)), output.toLocaleString(), percentage(output, totalOutputTokens), money(outputCost(row)), money(inputCost(row) + outputCost(row))];
        });
        tokenTableRows.push(["TOTAL (TOKEN-BASED)", "", totalInputTokens.toLocaleString(), "", money(totalInputCost), totalOutputTokens.toLocaleString(), "", money(totalOutputCost), money(totalInputCost + totalOutputCost)]);
        diagnosticsSummary.append(createSummaryTable("Token consumption", ["Step", "Elapsed", "Input tokens", "Input %", "Input cost", "Output tokens", "Output %", "Output cost", "Total cost"], tokenTableRows.slice(0, -1), tokenTableRows.at(-1)));

        const hourlyCost = row => (row.cost.elapsed ? parseDuration(row.cost.elapsed) : 0) / 3600000 * Number(row.cost.costPerHour || 0);
        const hourlyRowsForTable = hourlyRows.map(row => [row.step.stepName, summaryElapsed(row.step), money(row.cost.costPerHour || 0), money(hourlyCost(row))]);
        const totalHourlyCost = hourlyRows.reduce((total, row) => total + hourlyCost(row), 0);
        if (hourlyRows.length) {
            const hourlyTable = createSummaryTable("Hourly consumption", ["Step", "Elapsed", "Rate / hour", "Total cost"], hourlyRowsForTable, ["TOTAL (HOURLY-BASED)", "", "", money(totalHourlyCost)]);
            hourlyTable.classList.add("diagnostics-summary-hourly");
            diagnosticsSummary.append(hourlyTable);
        }

        const totalElapsed = workflowSummary.mainPipelineStepsData.reduce((total, step) => total + Math.max(0, Date.parse(step.completedOnUtc) - Date.parse(step.startedOnUtc)), 0);
        const metrics = document.createElement("div");
        metrics.className = "diagnostics-summary-metrics";
        const elapsed = document.createElement("span");
        elapsed.append("Total elapsed workflow time: ");
        const elapsedValue = document.createElement("strong");
        elapsedValue.textContent = formatDurationMilliseconds(totalElapsed);
        elapsed.append(elapsedValue);
        const total = document.createElement("span");
        total.append("Total cost: ");
        const totalValue = document.createElement("strong");
        totalValue.textContent = money(totalInputCost + totalOutputCost + totalHourlyCost);
        total.append(totalValue);
        metrics.append(elapsed, total);
        diagnosticsSummary.append(metrics);
    }

    function parseDuration(value) {
        if (typeof value !== "string") return 0;
        const match = value.match(/^(-?\d+):([0-5]\d):([0-5]\d)(?:\.(\d+))?$/);
        if (!match) return 0;
        return ((Number(match[1]) * 60 + Number(match[2])) * 60 + Number(match[3])) * 1000 + Number(`0.${match[4] || "0"}`) * 1000;
    }

    function createParameterGrid(title, parameters) {
        const section = document.createElement("section");
        section.className = "diagnostics-parameters";
        const heading = document.createElement("h4");
        heading.textContent = title;
        const grid = document.createElement("div");
        grid.className = "diagnostics-parameter-grid";
        for (const parameter of parameters) {
            const name = document.createElement("div");
            name.className = "diagnostics-parameter-name";
            name.textContent = parameter.name;
            const value = document.createElement("div");
            value.className = "diagnostics-parameter-value";
            value.textContent = parameter.value;
            grid.append(name, value);
        }
        section.append(heading, grid);
        return section;
    }

    function getDiagnosticsSteps() {
        const diagnosticsSteps = [];
        for (const event of diagnosticsEvents) {
            const payload = readPayload(event.rawData);
            if (event.eventType === "workflowCompleted") {
                continue;
            } else if (event.eventType === "workflowStepStarted") {
                diagnosticsSteps.push({
                    name: typeof payload.stepName === "string" ? payload.stepName : "Unnamed step",
                    type: "Code",
                    inputs: parameterRows(payload.inputParameters),
                    outputs: [],
                    complete: false,
                    elapsed: "",
                    errorMessage: ""
                });
            } else if (event.eventType === "workflowStepCompleted") {
                const step = diagnosticsSteps.find(candidate => !candidate.complete && candidate.name === payload.stepName);
                if (step) {
                    step.complete = true;
                    step.type = payload.isAgentic ? "Agentic" : "Code";
                    step.outputs = parameterRows(Array.isArray(payload.parametersDiff) ? payload.parametersDiff.map(parameter => ({ name: parameter.name, value: parameter.newValue })) : []);
                    step.elapsed = formatElapsed(payload.elapsed);
                }
            } else if (event.eventType === "workflowError") {
                for (const step of diagnosticsSteps) {
                    if (!step.complete) step.errorMessage = typeof payload.errorMessage === "string" ? payload.errorMessage : "Workflow failed.";
                }
            }
        }
        return diagnosticsSteps;
    }

    function renderDiagnosticsDetails() {
        const diagnosticsSteps = getDiagnosticsSteps();
        if (selectedDiagnosticsStep >= diagnosticsSteps.length) selectedDiagnosticsStep = -1;
        if (selectedDiagnosticsStep === -1 && diagnosticsSteps.length) selectedDiagnosticsStep = 0;
        diagnosticsStepList.replaceChildren();
        diagnosticsStepDetails.replaceChildren();
        diagnosticsSteps.forEach((step, index) => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "diagnostics-step";
            button.classList.toggle("is-selected", index === selectedDiagnosticsStep);
            button.setAttribute("aria-pressed", String(index === selectedDiagnosticsStep));
            const name = document.createElement("strong");
            name.textContent = step.name;
            const status = document.createElement("span");
            status.textContent = step.errorMessage ? "Error" : step.complete ? `${step.type} · complete` : `${step.type} · in progress`;
            button.append(name, status);
            button.addEventListener("click", () => {
                selectedDiagnosticsStep = index;
                renderDiagnosticsDetails();
            });
            diagnosticsStepList.append(button);
        });

        const step = diagnosticsSteps[selectedDiagnosticsStep];
        if (!step) {
            const empty = document.createElement("p");
            empty.className = "diagnostics-empty";
            empty.textContent = "No workflow steps received yet.";
            diagnosticsStepDetails.append(empty);
            return;
        }

        const heading = document.createElement("div");
        heading.className = "diagnostics-detail-heading";
        const title = document.createElement("h3");
        title.textContent = step.name;
        const type = document.createElement("span");
        type.textContent = step.type;
        heading.append(title, type);
        diagnosticsStepDetails.append(heading, createParameterGrid("Inputs", step.inputs));
        if (step.errorMessage) {
            const errorMessage = document.createElement("p");
            errorMessage.className = "diagnostics-step-error";
            errorMessage.textContent = `Workflow error: ${step.errorMessage}`;
            diagnosticsStepDetails.append(errorMessage);
        } else if (step.complete) {
            diagnosticsStepDetails.append(createParameterGrid("Outputs", step.outputs));
            const elapsed = document.createElement("p");
            elapsed.className = "diagnostics-elapsed";
            elapsed.textContent = `Elapsed: ${step.elapsed}`;
            diagnosticsStepDetails.append(elapsed);
        } else {
            const pending = document.createElement("p");
            pending.className = "diagnostics-pending";
            pending.textContent = "Step is still in progress.";
            diagnosticsStepDetails.append(pending);
        }
    }

    function appendDiagnostics(event) {
        if (!event.rawData) return;
        diagnosticsEvents.push({ eventType: event.eventType, rawData: event.rawData });
        diagnosticsOutput.value = diagnosticsEvents.map(eventChunk => `{"eventType":${JSON.stringify(eventChunk.eventType)},"payload":${eventChunk.rawData}}`).join("\n\n");
        diagnosticsOutput.scrollTop = diagnosticsOutput.scrollHeight;
        renderDiagnosticsDetails();
    }

    function resetDiagnostics() {
        diagnosticsEvents.length = 0;
        selectedDiagnosticsStep = -1;
        workflowSummary = null;
        diagnosticsOutput.value = "";
        renderDiagnosticsDetails();
        renderDiagnosticsSummary();
    }

    setConfigurationVisible(false);

    connection.on("State", state => {
        renderMessages(state.messages);
        tokenCount.textContent = `${state.tokenCount} tokens`;
        costCount.textContent = `$${Number(state.cumulatedCost).toFixed(2)}`;
    });
    connection.on("Progress", event => {
        progress.textContent = event.message;
        appendDiagnostics(event);
        showError("");
    });
    connection.on("WorkflowSummary", summary => {
        workflowSummary = summary;
        renderDiagnosticsSummary();
    });
    connection.on("Operation", setOperation);
    connection.on("Error", showError);
    connection.onreconnected(() => connection.invoke("Initialize", chatId));

    composer.addEventListener("submit", async event => {
        event.preventDefault();
        const text = message.value.trim();
        if (!text || isActive) return;
        showError("");
        resetDiagnostics();
        renderPending(text);
        message.value = "";
        setOperation("chat");
        try {
            await connection.invoke("Submit", chatId, text);
        } catch (exception) {
            showError(exception.message || "Unable to submit the message.");
            setOperation("idle");
        }
    });
    message.addEventListener("input", () => {
        send.disabled = isActive || !message.value.trim();
    });
    message.addEventListener("keydown", event => {
        if (event.key !== "Enter" || event.shiftKey || event.ctrlKey || event.isComposing || event.keyCode === 229) return;
        event.preventDefault();
        composer.requestSubmit();
    });
    transcript.addEventListener("click", async event => {
        const copyButton = event.target.closest(".message-copy");
        if (!copyButton) return;

        try {
            await navigator.clipboard.writeText(copyButton.dataset.copyText);
            copyButton.textContent = "Copied";
            copyButton.title = "Copied";
            copyButton.setAttribute("aria-label", "Message copied");
            window.setTimeout(() => {
                copyButton.textContent = "Copy";
                copyButton.title = "Copy message";
                copyButton.setAttribute("aria-label", "Copy message");
            }, 1400);
        } catch {
            showError("Unable to copy the message.");
        }
    });
    stop.addEventListener("click", () => connection.invoke("Stop", chatId));
    newChat.addEventListener("click", () => {
        if (hasConversationContent && !window.confirm(discardWarning)) return;
        connection.invoke("NewChat", chatId);
    });
    window.addEventListener("beforeunload", event => {
        if (!hasConversationContent) return;
        event.preventDefault();
        event.returnValue = discardWarning;
    });
    configurationToggle.addEventListener("click", () => setConfigurationVisible(!configurationRail.classList.contains("is-open")));
    configurationClose.addEventListener("click", () => setConfigurationVisible(false));
    diagnosticsToggle.addEventListener("click", () => setDiagnosticsVisible(diagnosticsModal.hidden));
    diagnosticsClose.addEventListener("click", () => setDiagnosticsVisible(false));
    diagnosticsSummaryTab.addEventListener("click", () => setDiagnosticsTab("summary"));
    diagnosticsStepsTab.addEventListener("click", () => setDiagnosticsTab("steps"));
    diagnosticsRawTab.addEventListener("click", () => setDiagnosticsTab("raw"));
    for (const tab of [diagnosticsSummaryTab, diagnosticsStepsTab, diagnosticsRawTab]) {
        tab.addEventListener("keydown", event => {
            if (event.key === "ArrowRight" || event.key === "ArrowLeft") {
                event.preventDefault();
                const tabs = [diagnosticsSummaryTab, diagnosticsStepsTab, diagnosticsRawTab];
                const currentIndex = tabs.indexOf(tab);
                const offset = event.key === "ArrowRight" ? 1 : -1;
                const nextTab = tabs[(currentIndex + offset + tabs.length) % tabs.length];
                setDiagnosticsTab(nextTab === diagnosticsSummaryTab ? "summary" : nextTab === diagnosticsStepsTab ? "steps" : "raw");
                nextTab.focus();
            }
        });
    }

    renderDiagnosticsSummary();

    connection.start()
        .then(() => connection.invoke("Initialize", chatId))
        .catch(() => showError("Unable to connect to the chat service."));
})();