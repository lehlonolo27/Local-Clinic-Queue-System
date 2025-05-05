const connection = new signalR.HubConnectionBuilder()
    .withUrl("/queueHub")
    .build();

connection.on("QueueUpdated", () => {
    fetch('/Patient/GetQueue')
        .then(response => response.text())
        .then(html => document.getElementById("queueList").innerHTML = html);
});

connection.start().then(() => {
    console.log("Connected to QueueHub");
});
