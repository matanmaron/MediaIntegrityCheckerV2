const logEl = document.getElementById("log");
const runBtn = document.getElementById("runBtn");

runBtn.onclick = async () => {
    if(runBtn.innerText === "START") {
        runBtn.innerText = "STOP";
        await fetch("/scan/start", { method: "POST" });
    } else {
        runBtn.innerText = "START";
        await fetch("/scan/stop", { method: "POST" });
    }
};

document.getElementById("clearBtn").onclick = () => {
    logEl.innerText = "";
};

document.getElementById("copyBtn").onclick = () => {
    navigator.clipboard.writeText(logEl.innerText);
};

setInterval(async () => {
    const res = await fetch("/log");
    logEl.innerText = await res.text();
    logEl.scrollTop = logEl.scrollHeight;
}, 2500);
