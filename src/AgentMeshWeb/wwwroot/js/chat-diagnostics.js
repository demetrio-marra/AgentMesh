export function createDiagnosticsController() {
    const modal = document.getElementById("diagnostics-modal");
    const output = document.getElementById("diagnostics-output");
    const toggle = document.getElementById("diagnostics-toggle");
    const close = document.getElementById("diagnostics-close");
    const tabs = {
        summary: document.getElementById("diagnostics-summary-tab"),
        steps: document.getElementById("diagnostics-steps-tab"),
        parameters: document.getElementById("diagnostics-parameters-tab"),
        raw: document.getElementById("diagnostics-raw-tab"),
        configuration: document.getElementById("diagnostics-configuration-tab")
    };
    const panels = {
        summary: document.getElementById("diagnostics-summary-panel"),
        steps: document.getElementById("diagnostics-steps-panel"),
        parameters: document.getElementById("diagnostics-parameters-panel"),
        raw: document.getElementById("diagnostics-raw-panel"),
        configuration: document.getElementById("diagnostics-configuration-panel")
    };
    const summary = document.getElementById("diagnostics-summary");
    const stepList = document.getElementById("diagnostics-step-list");
    const stepDetails = document.getElementById("diagnostics-step-details");
    const parametersByStep = document.getElementById("diagnostics-parameters-by-step");
    const parameterDetailModal = document.getElementById("diagnostics-parameter-detail-modal");
    const parameterDetailTitle = document.getElementById("diagnostics-parameter-detail-title");
    const parameterDetailClose = document.getElementById("diagnostics-parameter-detail-close");
    const parameterDetailTable = document.getElementById("diagnostics-parameter-detail-table");
    const events = [];
    let selectedStep = -1;
    let selectedParameter = null;
    let workflowSummary = null;

    function readPayload(rawData) {
        try { return JSON.parse(rawData); } catch { return {}; }
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
        const fraction = Number(`0.${match[4] || "0"}`);
        if (Number(match[1]) === 0 && Number(match[2]) === 0 && Number(match[3]) === 0 && fraction < 1) return "<1s";
        return [Number(match[1]) ? `${Number(match[1])}h` : "", Number(match[2]) ? `${Number(match[2])}m` : "", Number(match[3]) || fraction ? `${Number(match[3])}s` : ""].filter(Boolean).join(" ") || "<1s";
    }

    function parseDuration(value) {
        if (typeof value !== "string") return 0;
        const match = value.match(/^(-?\d+):([0-5]\d):([0-5]\d)(?:\.(\d+))?$/);
        if (!match) return 0;
        return ((Number(match[1]) * 60 + Number(match[2])) * 60 + Number(match[3])) * 1000 + Number(`0.${match[4] || "0"}`) * 1000;
    }

    function summaryElapsed(step) {
        return formatDurationMilliseconds(Date.parse(step.completedOnUtc) - Date.parse(step.startedOnUtc));
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

    function cell(row, value, tagName = "td") {
        const element = document.createElement(tagName);
        element.textContent = value;
        row.append(element);
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
        headers.forEach(header => cell(headerRow, header, "th"));
        head.append(headerRow);
        table.append(head);
        const body = document.createElement("tbody");
        rows.forEach(values => {
            const row = document.createElement("tr");
            values.forEach(value => cell(row, value));
            body.append(row);
        });
        table.append(body);
        const foot = document.createElement("tfoot");
        const totalRow = document.createElement("tr");
        totalValues.forEach(value => cell(totalRow, value));
        foot.append(totalRow);
        table.append(foot);
        wrapper.append(table);
        return wrapper;
    }

    function renderSummary() {
        summary.replaceChildren();
        const workflowSteps = workflowSummary?.mainPipelineStepsData;
        if (!Array.isArray(workflowSteps) || !workflowSteps.length) {
            const empty = document.createElement("p");
            empty.className = "diagnostics-summary-empty";
            empty.textContent = "No completed workflow summary is available.";
            summary.append(empty);
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
        for (const step of workflowSteps) {
            if (!step.isAgentic) { tokenRows.push({ step, cost: null }); continue; }
            const queue = costsByAgent.get(step.agentName || "");
            const cost = queue?.length ? queue.shift() : null;
            if (cost?.costPerHour != null) hourlyRows.push({ step, cost });
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
            if (!step.isAgentic) return [step.stepName, summaryElapsed(step), "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A"];
            const input = step.inputTokens || 0;
            const output = step.outputTokens || 0;
            return [step.stepName, summaryElapsed(step), input.toLocaleString(), percentage(input, totalInputTokens), money(inputCost(row)), output.toLocaleString(), percentage(output, totalOutputTokens), money(outputCost(row)), money(inputCost(row) + outputCost(row))];
        });
        tokenTableRows.push(["TOTAL (TOKEN-BASED)", "", totalInputTokens.toLocaleString(), "", money(totalInputCost), totalOutputTokens.toLocaleString(), "", money(totalOutputCost), money(totalInputCost + totalOutputCost)]);
        summary.append(createSummaryTable("Token consumption", ["Step", "Elapsed", "Input tokens", "Input %", "Input cost", "Output tokens", "Output %", "Output cost", "Total cost"], tokenTableRows.slice(0, -1), tokenTableRows.at(-1)));
        const hourlyCost = row => (row.cost.elapsed ? parseDuration(row.cost.elapsed) : 0) / 3600000 * Number(row.cost.costPerHour || 0);
        const hourlyRowsForTable = hourlyRows.map(row => [row.step.stepName, summaryElapsed(row.step), money(row.cost.costPerHour || 0), money(hourlyCost(row))]);
        const totalHourlyCost = hourlyRows.reduce((total, row) => total + hourlyCost(row), 0);
        if (hourlyRows.length) {
            const hourlyTable = createSummaryTable("Hourly consumption", ["Step", "Elapsed", "Rate / hour", "Total cost"], hourlyRowsForTable, ["TOTAL (HOURLY-BASED)", "", "", money(totalHourlyCost)]);
            hourlyTable.classList.add("diagnostics-summary-hourly");
            summary.append(hourlyTable);
        }
        const totalElapsed = workflowSteps.reduce((total, step) => total + Math.max(0, Date.parse(step.completedOnUtc) - Date.parse(step.startedOnUtc)), 0);
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
        summary.append(metrics);
    }

    function getSteps() {
        const steps = [];
        for (const event of events) {
            const payload = readPayload(event.rawData);
            if (event.eventType === "workflowStepStarted") steps.push({ name: typeof payload.stepName === "string" ? payload.stepName : "Unnamed step", type: "Code", inputs: parameterRows(payload.inputParameters), outputs: [], complete: false, elapsed: "", errorMessage: "" });
            else if (event.eventType === "workflowStepCompleted") {
                const step = steps.find(candidate => !candidate.complete && candidate.name === payload.stepName);
                if (step) { step.complete = true; step.type = payload.isAgentic ? "Agentic" : "Code"; step.outputs = parameterRows(Array.isArray(payload.parametersDiff) ? payload.parametersDiff.map(parameter => ({ name: parameter.name, value: parameter.newValue })) : []); step.elapsed = formatElapsed(payload.elapsed); }
            } else if (event.eventType === "workflowError") steps.filter(step => !step.complete).forEach(step => step.errorMessage = payload.errorMessage || "Workflow failed.");
        }
        return steps;
    }

    function createParameterGrid(title, parameters) {
        const section = document.createElement("section");
        section.className = "diagnostics-parameters";
        const heading = document.createElement("h4");
        heading.textContent = title;
        const grid = document.createElement("div");
        grid.className = "diagnostics-parameter-grid";
        parameters.forEach(parameter => {
            const name = document.createElement("div"); name.className = "diagnostics-parameter-name"; name.textContent = parameter.name;
            const value = document.createElement("div"); value.className = "diagnostics-parameter-value"; value.textContent = parameter.value;
            grid.append(name, value);
        });
        section.append(heading, grid);
        return section;
    }

    function renderSteps() {
        const steps = getSteps();
        if (selectedStep >= steps.length) selectedStep = -1;
        if (selectedStep === -1 && steps.length) selectedStep = 0;
        stepList.replaceChildren(); stepDetails.replaceChildren();
        steps.forEach((step, index) => {
            const button = document.createElement("button");
            button.type = "button"; button.className = "diagnostics-step"; button.classList.toggle("is-selected", index === selectedStep); button.setAttribute("aria-pressed", String(index === selectedStep));
            const name = document.createElement("strong"); name.textContent = step.name;
            const status = document.createElement("span"); status.textContent = step.errorMessage ? "Error" : step.complete ? `${step.type} · complete` : `${step.type} · in progress`;
            button.append(name, status); button.addEventListener("click", () => { selectedStep = index; renderSteps(); }); stepList.append(button);
        });
        const step = steps[selectedStep];
        if (!step) { const empty = document.createElement("p"); empty.className = "diagnostics-empty"; empty.textContent = "No workflow steps received yet."; stepDetails.append(empty); return; }
        const heading = document.createElement("div"); heading.className = "diagnostics-detail-heading";
        const title = document.createElement("h3"); title.textContent = step.name;
        const type = document.createElement("span"); type.textContent = step.type;
        heading.append(title, type); stepDetails.append(heading, createParameterGrid("Inputs", step.inputs));
        if (step.errorMessage) { const error = document.createElement("p"); error.className = "diagnostics-step-error"; error.textContent = `Workflow error: ${step.errorMessage}`; stepDetails.append(error); }
        else if (step.complete) { stepDetails.append(createParameterGrid("Outputs", step.outputs)); const elapsed = document.createElement("p"); elapsed.className = "diagnostics-elapsed"; elapsed.textContent = `Elapsed: ${step.elapsed}`; stepDetails.append(elapsed); }
        else { const pending = document.createElement("p"); pending.className = "diagnostics-pending"; pending.textContent = "Step is still in progress."; stepDetails.append(pending); }
    }

    function deriveParameterMatrix() {
        const steps = [];
        const parameterNames = [];
        const seenParameters = new Set();
        events.forEach(event => {
            const payload = readPayload(event.rawData);
            if (event.eventType === "workflowStepStarted") steps.push({ name: payload.stepName || "Unnamed step", parameters: [], complete: false });
            else if (event.eventType === "workflowStepCompleted") {
                const step = steps.find(candidate => !candidate.complete && candidate.name === payload.stepName);
                if (step) {
                    step.complete = true;
                    step.parameters = parameterRows(Array.isArray(payload.parametersDiff) ? payload.parametersDiff.map(parameter => ({ name: parameter.name, value: parameter.newValue })) : []);
                    step.parameters.forEach(parameter => {
                        if (seenParameters.has(parameter.name)) return;
                        seenParameters.add(parameter.name);
                        parameterNames.push(parameter.name);
                    });
                }
            }
        });

        return {
            steps,
            rows: parameterNames.map(name => ({
                name,
                values: steps.map(step => step.parameters.find(parameter => parameter.name === name)?.value ?? "")
            }))
        };
    }

    function createParameterTable(matrix, rows, onSelect = null) {
        const wrapper = document.createElement("div"); wrapper.className = "diagnostics-parameters-table-wrap";
        const table = document.createElement("table"); table.className = "diagnostics-parameters-table";
        const header = document.createElement("tr"); cell(header, "Parameter", "th"); matrix.steps.forEach(step => cell(header, step.name, "th"));
        const head = document.createElement("thead"); head.append(header); table.append(head);
        const body = document.createElement("tbody");
        rows.forEach(parameter => {
            const row = document.createElement("tr");
            const name = document.createElement("th"); name.scope = "row";
            if (onSelect) {
                const button = document.createElement("button");
                button.type = "button"; button.className = "diagnostics-parameter-select"; button.dataset.parameterName = parameter.name; button.textContent = parameter.name;
                button.addEventListener("click", () => onSelect(parameter.name));
                name.append(button);
            } else name.textContent = parameter.name;
            row.append(name);
            parameter.values.forEach(value => cell(row, value));
            body.append(row);
        });
        table.append(body); wrapper.append(table);
        return wrapper;
    }

    function renderParameterDetail(matrix) {
        if (selectedParameter === null) return;
        const parameter = matrix.rows.find(row => row.name === selectedParameter);
        if (!parameter) return;
        parameterDetailTitle.textContent = `Parameter transitions: ${selectedParameter}`;
        const populatedColumns = parameter.values
            .map((value, index) => ({ value, step: matrix.steps[index] }))
            .filter(column => column.value !== "");
        const detailMatrix = {
            steps: populatedColumns.map(column => column.step)
        };
        parameterDetailTable.append(createParameterTable(detailMatrix, [{
            name: parameter.name,
            values: populatedColumns.map(column => column.value)
        }]));
    }

    function renderParameters() {
        parametersByStep.replaceChildren();
        parameterDetailTable.replaceChildren();
        const matrix = deriveParameterMatrix();
        if (!matrix.steps.length || !matrix.rows.length) { const empty = document.createElement("p"); empty.className = "diagnostics-empty"; empty.textContent = "No step parameters received yet."; parametersByStep.append(empty); return; }
        parametersByStep.append(createParameterTable(matrix, matrix.rows, openParameterDetail));
        renderParameterDetail(matrix);
    }

    function openParameterDetail(name) {
        selectedParameter = name;
        renderParameters();
        parameterDetailModal.hidden = false;
        parameterDetailClose.focus();
    }

    function closeParameterDetail() {
        if (parameterDetailModal.hidden) return;
        const closedParameter = selectedParameter;
        parameterDetailModal.hidden = true;
        selectedParameter = null;
        parameterDetailTitle.textContent = "Parameter details";
        [...parametersByStep.querySelectorAll(".diagnostics-parameter-select")]
            .find(button => button.dataset.parameterName === closedParameter)?.focus();
    }

    function setTab(tab) {
        Object.entries(tabs).forEach(([name, element]) => { const selected = name === tab; element.classList.toggle("is-selected", selected); element.setAttribute("aria-selected", String(selected)); element.tabIndex = selected ? 0 : -1; panels[name].hidden = !selected; });
        if (tab === "raw") output.focus();
    }

    function setVisible(visible) {
        if (!visible) {
            parameterDetailModal.hidden = true;
            selectedParameter = null;
            parameterDetailTitle.textContent = "Parameter details";
        }
        modal.hidden = !visible; toggle.setAttribute("aria-expanded", String(visible));
        if (visible) { setTab("summary"); tabs.summary.focus(); }
    }

    function append(event) {
        if (!event.rawData) return;
        events.push({ eventType: event.eventType, rawData: event.rawData });
        output.value = events.map(item => `{"eventType":${JSON.stringify(item.eventType)},"payload":${item.rawData}}`).join("\n\n");
        output.scrollTop = output.scrollHeight; renderSteps(); renderParameters();
    }

    function reset() { events.length = 0; selectedStep = -1; selectedParameter = null; parameterDetailModal.hidden = true; parameterDetailTitle.textContent = "Parameter details"; workflowSummary = null; output.value = ""; renderSteps(); renderParameters(); renderSummary(); }
    function setSummary(value) { workflowSummary = value; renderSummary(); }

    toggle.addEventListener("click", () => setVisible(modal.hidden));
    close.addEventListener("click", () => setVisible(false));
    parameterDetailClose.addEventListener("click", closeParameterDetail);
    parameterDetailModal.addEventListener("click", event => { if (event.target === parameterDetailModal) closeParameterDetail(); });
    Object.entries(tabs).forEach(([name, tab]) => tab.addEventListener("click", () => setTab(name)));
    Object.entries(tabs).forEach(([name, tab]) => tab.addEventListener("keydown", event => {
        if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
        event.preventDefault();
        const names = Object.keys(tabs);
        const next = names[(names.indexOf(name) + (event.key === "ArrowRight" ? 1 : -1) + names.length) % names.length];
        setTab(next); tabs[next].focus();
    }));
    renderSteps(); renderParameters(); renderSummary();

    return { append, reset, setSummary };
}
