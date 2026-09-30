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
    const discardWarning = "The conversation will be lost. Continue?";
    let isActive = false;
    let hasConversationContent = false;

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

    setConfigurationVisible(false);

    connection.on("State", state => {
        renderMessages(state.messages);
        tokenCount.textContent = `${state.tokenCount} tokens`;
        costCount.textContent = `$${Number(state.cumulatedCost).toFixed(2)}`;
    });
    connection.on("Progress", event => {
        progress.textContent = event.message;
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

    connection.start()
        .then(() => connection.invoke("Initialize", chatId))
        .catch(() => showError("Unable to connect to the chat service."));
})();