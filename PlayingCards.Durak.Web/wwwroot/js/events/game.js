"use strict";

var gameHubConnection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();

gameHubConnection.on("ChangeStatus", function (user, message) {
    getStatus();
});

gameHubConnection.start().then(function () {
}).catch(function (err) {
    return console.error(err.toString());
});
