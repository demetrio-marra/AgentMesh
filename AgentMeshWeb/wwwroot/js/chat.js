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
    const diagnosticsStepsTab = document.getElementById("diagnostics-steps-tab");
    const diagnosticsRawTab = document.getElementById("diagnostics-raw-tab");
    const diagnosticsStepsPanel = document.getElementById("diagnostics-steps-panel");
    const diagnosticsRawPanel = document.getElementById("diagnostics-raw-panel");
    const diagnosticsStepList = document.getElementById("diagnostics-step-list");
    const diagnosticsStepDetails = document.getElementById("diagnostics-step-details");
    const discardWarning = "The conversation will be lost. Continue?";
    const diagnosticsEvents = [];
    let isActive = false;
    let hasConversationContent = false;
    let selectedDiagnosticsStep = -1;

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
            setDiagnosticsTab("steps");
            diagnosticsStepsTab.focus();
        }
    }

    function setDiagnosticsTab(tab) {
        const showSteps = tab === "steps";
        diagnosticsStepsTab.classList.toggle("is-selected", showSteps);
        diagnosticsRawTab.classList.toggle("is-selected", !showSteps);
        diagnosticsStepsTab.setAttribute("aria-selected", String(showSteps));
        diagnosticsRawTab.setAttribute("aria-selected", String(!showSteps));
        diagnosticsStepsTab.tabIndex = showSteps ? 0 : -1;
        diagnosticsRawTab.tabIndex = showSteps ? -1 : 0;
        diagnosticsStepsPanel.hidden = !showSteps;
        diagnosticsRawPanel.hidden = showSteps;
        if (!showSteps) diagnosticsOutput.focus();
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
            if (event.eventType === "workflowStepStarted") {
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
        diagnosticsOutput.value = "";
        renderDiagnosticsDetails();
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
    diagnosticsStepsTab.addEventListener("click", () => setDiagnosticsTab("steps"));
    diagnosticsRawTab.addEventListener("click", () => setDiagnosticsTab("raw"));
    for (const tab of [diagnosticsStepsTab, diagnosticsRawTab]) {
        tab.addEventListener("keydown", event => {
            if (event.key === "ArrowRight" || event.key === "ArrowLeft") {
                event.preventDefault();
                const nextTab = tab === diagnosticsStepsTab ? diagnosticsRawTab : diagnosticsStepsTab;
                setDiagnosticsTab(nextTab === diagnosticsStepsTab ? "steps" : "raw");
                nextTab.focus();
            }
        });
    }

    connection.start()
        .then(() => connection.invoke("Initialize", chatId))
        .catch(() => showError("Unable to connect to the chat service."));
})();