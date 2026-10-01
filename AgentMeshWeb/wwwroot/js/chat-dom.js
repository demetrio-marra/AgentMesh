export function createMessageRenderer({ transcript }) {
    let pendingRequest = null;

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

    function scrollToBottom() {
        transcript.scrollTop = transcript.scrollHeight;
    }

    function renderMessages(messages) {
        transcript.replaceChildren();
        if (!messages.length) {
            const empty = document.createElement("div");
            empty.className = "empty-state";
            empty.textContent = "Conversation is empty";
            transcript.append(empty);
            scrollToBottom();
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
            body.innerHTML = window.DOMPurify.sanitize(window.marked.parse(messageItem.text));
            const actions = document.createElement("div");
            actions.className = "message-actions";
            actions.append(createCopyButton(messageItem.text));
            article.append(role, body, actions);
            transcript.append(article);
        }
        scrollToBottom();
    }

    function renderAcceptedUser(text) {
        transcript.querySelector(".empty-state")?.remove();
        const article = document.createElement("article");
        article.className = "message message-user";
        const role = document.createElement("div");
        role.className = "message-role";
        role.textContent = "User";
        const body = document.createElement("div");
        body.className = "message-body";
        body.textContent = text;
        const actions = document.createElement("div");
        actions.className = "message-actions";
        actions.append(createCopyButton(text));
        article.append(role, body, actions);
        transcript.append(article);
        scrollToBottom();
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

    function renderPlaceholder() {
        if (!pendingRequest) return;
        pendingRequest.state.textContent = pendingRequest.displayText;
        pendingRequest.elapsed.textContent = formatDurationMilliseconds(Date.now() - pendingRequest.stateChangedAt);
    }

    function createPlaceholder() {
        const article = document.createElement("article");
        article.className = "message message-assistant pending";
        const body = document.createElement("div");
        body.className = "message-body";
        const state = document.createElement("em");
        const spinner = document.createElement("span");
        spinner.className = "placeholder-spinner";
        spinner.setAttribute("aria-hidden", "true");
        body.append(state, spinner);
        const elapsed = document.createElement("small");
        elapsed.className = "message-elapsed";
        article.append(body, elapsed);
        transcript.append(article);
        pendingRequest = { article, body, state, elapsed, displayText: "Processing", stateChangedAt: Date.now(), timer: window.setInterval(renderPlaceholder, 1000) };
        renderPlaceholder();
        scrollToBottom();
    }

    function updatePlaceholder(text) {
        if (!pendingRequest || !text || pendingRequest.displayText === text) return;
        pendingRequest.displayText = text;
        pendingRequest.stateChangedAt = Date.now();
        renderPlaceholder();
    }

    function clearPendingRequest() {
        if (!pendingRequest) return;
        window.clearInterval(pendingRequest.timer);
        pendingRequest.article.remove();
        pendingRequest = null;
    }

    function renderFailure() {
        if (!pendingRequest) return;
        window.clearInterval(pendingRequest.timer);
        pendingRequest.article.classList.remove("pending");
        pendingRequest.article.classList.add("message-error");
        pendingRequest.body.replaceChildren();
        const text = document.createElement("em");
        text.textContent = "An error has occurred";
        pendingRequest.body.append(text);
        pendingRequest.elapsed.remove();
        pendingRequest = null;
    }

    function appendCopyHandler(event) {
        const copyButton = event.target.closest(".message-copy");
        if (!copyButton) return;
        navigator.clipboard.writeText(copyButton.dataset.copyText).then(() => {
            copyButton.textContent = "Copied";
            copyButton.title = "Copied";
            copyButton.setAttribute("aria-label", "Message copied");
            window.setTimeout(() => {
                copyButton.textContent = "Copy";
                copyButton.title = "Copy message";
                copyButton.setAttribute("aria-label", "Copy message");
            }, 1400);
        }).catch(() => { });
    }

    transcript.addEventListener("click", appendCopyHandler);

    return {
        renderMessages,
        renderAcceptedUser,
        createPlaceholder,
        updatePlaceholder,
        clearPendingRequest,
        renderFailure,
        appendPendingRequest: () => {
            if (pendingRequest) {
                transcript.append(pendingRequest.article);
                scrollToBottom();
            }
        },
        get hasPendingRequest() {
            return pendingRequest !== null;
        }
    };
}
