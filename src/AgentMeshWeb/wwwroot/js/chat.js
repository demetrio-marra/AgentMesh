import { createMessageRenderer } from "./chat-dom.js";
import { createDiagnosticsController } from "./chat-diagnostics.js";
import { createNotificationController } from "./chat-notifications.js";

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
    const tokenCount = document.getElementById("token-count");
    const costCount = document.getElementById("cost-count");
    const discardWarning = "The conversation will be lost. Continue?";
    const renderer = createMessageRenderer({ transcript });
    const diagnostics = createDiagnosticsController();
    const notifications = createNotificationController({ toastRegion: document.getElementById("terminal-toasts") });
    const connection = new window.signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();
    let isActive = false;
    let hasConversationContent = false;
    let isSubmitting = false;
    let committedMessageCount = 0;
    let requestInProgress = false;
    let terminalNotified = false;

    function setOperation(state) {
        isActive = state === "chat" || state === "summarizing";
        if (isActive) {
            requestInProgress = true;
            terminalNotified = false;
        }
        message.disabled = isActive || isSubmitting;
        send.hidden = isActive;
        stop.hidden = !isActive;
        send.disabled = isActive || isSubmitting || !message.value.trim();
    }

    function finishComposer() {
        isSubmitting = false;
        setOperation("idle");
        message.focus();
    }

    function resetChat() {
        if (hasConversationContent && !window.confirm(discardWarning)) return;
        requestInProgress = false;
        terminalNotified = true;
        renderer.clearPendingRequest();
        connection.invoke("NewChat", chatId);
    }

    connection.on("State", state => {
        const previousCommittedMessageCount = committedMessageCount;
        renderer.renderMessages(state.messages);
        committedMessageCount = state.messages.length;
        hasConversationContent = state.messages.length > 0;
        tokenCount.textContent = `${state.tokenCount} tokens`;
        costCount.textContent = `$${Number(state.cumulatedCost).toFixed(2)}`;
        if (renderer.hasPendingRequest && state.messages.length > previousCommittedMessageCount) {
            renderer.clearPendingRequest();
            finishComposer();
        } else if (renderer.hasPendingRequest) {
            renderer.appendPendingRequest();
        }
    });
    connection.on("Progress", event => {
        renderer.updatePlaceholder(event.message);
        diagnostics.append(event);
    });
    connection.on("WorkflowSummary", diagnostics.setSummary);
    connection.on("Operation", setOperation);
    connection.on("Error", renderer.renderFailure);
    connection.on("Operation", state => {
        setOperation(state);
        if (state === "canceled") {
            requestInProgress = false;
            terminalNotified = true;
            renderer.clearPendingRequest();
            finishComposer();
        } else if (state === "failed") {
            renderer.renderFailure();
            if (requestInProgress && !terminalNotified) {
                terminalNotified = true;
                requestInProgress = false;
                notifications.notify("failed");
            }
        } else if (state === "idle" && requestInProgress && !terminalNotified) {
            terminalNotified = true;
            requestInProgress = false;
            notifications.notify("completed");
        }
    });
    connection.onreconnected(() => {
        requestInProgress = false;
        terminalNotified = true;
        renderer.clearPendingRequest();
        finishComposer();
        connection.invoke("Initialize", chatId);
    });

    composer.addEventListener("submit", async event => {
        event.preventDefault();
        const text = message.value.trim();
        if (!text || isActive || isSubmitting) return;
        diagnostics.reset();
        isSubmitting = true;
        setOperation("idle");
        try {
            await connection.invoke("Submit", chatId, text);
            renderer.renderAcceptedUser(text);
            hasConversationContent = true;
            renderer.createPlaceholder();
            message.value = "";
            setOperation("chat");
        } catch {
            finishComposer();
        }
    });
    message.addEventListener("input", () => {
        send.disabled = isActive || isSubmitting || !message.value.trim();
    });
    message.addEventListener("keydown", event => {
        if (event.key !== "Enter" || event.shiftKey || event.ctrlKey || event.isComposing || event.keyCode === 229) return;
        event.preventDefault();
        composer.requestSubmit();
    });
    stop.addEventListener("click", () => connection.invoke("Stop", chatId));
    newChat.addEventListener("click", resetChat);
    window.addEventListener("beforeunload", event => {
        if (!hasConversationContent) return;
        event.preventDefault();
        event.returnValue = discardWarning;
    });

    setOperation("idle");
    connection.start()
        .then(() => connection.invoke("Initialize", chatId))
        .catch(finishComposer);
})();
