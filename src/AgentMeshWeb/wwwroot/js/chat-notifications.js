export function createNotificationController({ toastRegion }) {
    const defaultTitle = document.title;
    let toast = null;
    let elapsedVisibleFocused = 0;
    let lastTick = null;
    let windowFocused = document.hasFocus();
    let titleFlashTimer = null;

    function isVisibleAndFocused() {
        return document.visibilityState === "visible" && windowFocused;
    }

    function restoreTitle() {
        if (titleFlashTimer !== null) {
            window.clearInterval(titleFlashTimer);
            titleFlashTimer = null;
        }
        document.title = defaultTitle;
    }

    function flashTitle(message) {
        restoreTitle();
        let showOutcome = true;
        document.title = `${defaultTitle} - ${message}`;
        titleFlashTimer = window.setInterval(() => {
            document.title = showOutcome ? defaultTitle : `${defaultTitle} - ${message}`;
            showOutcome = !showOutcome;
        }, 1000);
    }

    function updateToastTimer() {
        const now = Date.now();
        if (lastTick !== null && toast && isVisibleAndFocused()) {
            elapsedVisibleFocused += now - lastTick;
            if (elapsedVisibleFocused >= 10000) {
                dismissToast();
            }
        }
        lastTick = now;
    }

    function dismissToast() {
        toast?.remove();
        toast = null;
        elapsedVisibleFocused = 0;
        lastTick = null;
    }

    function requestPermission() {
        if (!("Notification" in window) || Notification.permission !== "default") return;
        try {
            const permissionRequest = Notification.requestPermission();
            permissionRequest?.catch(() => { });
        } catch { }
    }

    function showDesktopNotification(message) {
        if (!("Notification" in window) || Notification.permission !== "granted") return;
        if (document.visibilityState === "visible" && windowFocused) return;
        try {
            const notification = new Notification("AgentMesh", {
                body: message,
                tag: "agentmesh-notification",
                requireInteraction: true,
                silent: false,
                timestamp: Date.now()
            });
            notification.addEventListener("click", () => {
                notification.close();
                window.focus();
            });
        } catch { }
    }

    function showToast(message, kind) {
        dismissToast();
        const nextToast = document.createElement("div");
        nextToast.className = `terminal-toast terminal-toast-${kind}`;
        nextToast.setAttribute("role", kind === "error" ? "alert" : "status");
        const text = document.createElement("span");
        text.textContent = message;
        const close = document.createElement("button");
        close.type = "button";
        close.className = "terminal-toast-close";
        close.setAttribute("aria-label", "Dismiss notification");
        close.textContent = "x";
        close.addEventListener("click", dismissToast);
        nextToast.append(text, close);
        toastRegion.append(nextToast);
        toast = nextToast;
        elapsedVisibleFocused = 0;
        lastTick = Date.now();
    }

    function notify(outcome) {
        const message = outcome === "completed" ? "Request completed" : "Request failed";
        showToast(message, outcome === "completed" ? "success" : "error");
        showDesktopNotification(message);
        if (document.visibilityState === "hidden") {
            flashTitle(message);
        }
    }

    document.addEventListener("visibilitychange", () => {
        updateToastTimer();
        if (document.visibilityState === "visible") restoreTitle();
    });
    window.addEventListener("focus", () => {
        windowFocused = true;
        updateToastTimer();
        restoreTitle();
    });
    window.addEventListener("blur", () => {
        windowFocused = false;
        updateToastTimer();
    });
    window.setInterval(updateToastTimer, 250);
    document.addEventListener("pointerdown", requestPermission);
    document.addEventListener("keydown", requestPermission);
    requestPermission();

    return { notify };
}