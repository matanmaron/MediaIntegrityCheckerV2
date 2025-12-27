const startStopBtn = document.getElementById("startStopBtn");
const copyBtn = document.getElementById("copyBtn");
const logBox = document.getElementById("logBox");

let running = false;
let lastLog = "";

// Start/Stop button
startStopBtn.onclick = async () => {
    if (!running) {
        await fetch("/scan/start", { method: "POST" });
        running = true;
        startStopBtn.innerText = "STOP";
    } else {
        await fetch("/scan/stop", { method: "POST" });
        running = false;
        startStopBtn.innerText = "START";
    }
};

// Copy log button
copyBtn.onclick = () => {
    navigator.clipboard.writeText(logBox.innerText);
};

// Fetch current state on page load
async function fetchState() {
    const res = await fetch("/state");
    const data = await res.json();
    running = data.running;
    startStopBtn.innerText = running ? "STOP" : "START";
}
fetchState();

// Poll the log file every 2 seconds
setInterval(async () => {
    const res = await fetch("/log");
    const logText = await res.text();

    if (logText !== lastLog) {
        lastLog = logText;
        renderLog(logText);
    }
}, 2000);

// Render log with colors
function renderLog(text) {
    const lines = text.split("\n");
    logBox.innerHTML = "";

    for (let line of lines) {
        // Skip OK lines
        if (line.startsWith("[OK]")) continue;

        // Check if line already contains HTML (summary line)
        if (line.includes("<span")) {
            const span = document.createElement("span");
            span.innerHTML = line + "<br>";
            logBox.appendChild(span);
        } else {
            // Normal log line: use class for color
            const span = document.createElement("span");
            if (line.startsWith("[NEW]")) span.className = "green";
            else if (line.startsWith("[LOCK]")) span.className = "yellow";
            else if (line.startsWith("[BAD]")) span.className = "red";
            else span.className = ""; // any other text

            span.textContent = line + "\n"; // keep plain text safe
            logBox.appendChild(span);
        }
    }

    logBox.scrollTop = logBox.scrollHeight;
}

