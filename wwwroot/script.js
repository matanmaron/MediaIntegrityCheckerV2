const btn = document.getElementById("startStopBtn");
const copyBtn = document.getElementById("copyBtn");
const logBox = document.getElementById("logBox");

async function refreshState() {
    const s = await fetch("/state").then(r=>r.json());
    btn.innerText = s.running ? "STOP" : "START";
}

btn.onclick = async () => {
    if (btn.innerText === "START")
        await fetch("/scan/start", {method:"POST"});
    else
        await fetch("/scan/stop", {method:"POST"});
    setTimeout(refreshState, 500);
};

copyBtn.onclick = () => navigator.clipboard.writeText(logBox.innerText);

setInterval(refreshState, 1500);
setInterval(async()=> logBox.textContent = await (await fetch("/log")).text(), 1500);
