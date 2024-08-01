let user;
const authCookieName = 'auth_name';
const authCookieSecret = 'auth_secret';
let gameStatus = null;
let speakTimerIntervalId = null;

function init() {
    user = null;
    let cookieName = getCookie(authCookieName);
    let cookieSecret = getCookie(authCookieSecret);
    if (cookieName == null || cookieSecret == null) {
        document.getElementById('nameLabel').classList.add('hidden');
        document.getElementById('logoutBtn').classList.add('hidden');
        document.getElementById('loginBlock').classList.remove('hidden');
        document.getElementById('main').classList.add('hidden');
        document.getElementById('tableMain').classList.add('hidden');
        document.getElementById('accountContainer').classList.add('not-auth');
    } else {
        user = {
            name: cookieName,
            secret: cookieSecret,
        };
        document.getElementById('nameLabel').classList.remove('hidden');
        document.getElementById('logoutBtn').classList.remove('hidden');
        document.getElementById('loginBlock').classList.add('hidden');
        document.getElementById('main').classList.remove('hidden');
        document.getElementById('tableMain').classList.remove('hidden');
        document.getElementById('accountContainer').classList.remove('not-auth');
    }
    if (user != null) {
        document.getElementById('nameLabel').innerHTML = user.name;
    } else {
        document.getElementById('nameLabel').innerHTML = '';
    }

    getStatus();
}

init();

setInterval(function () {
    getStatus();
}, 1000);

function createTable() {
    SendRequest({
        method: 'Post',
        url: '/Home/CreateTable',
        body: {
            playerSecret: user.secret,
            playerName: user.name,
        },
        success(data) {
            gameStatus = null;
            getStatus();
        }
    });
}

function joinToTableClick(elem) {
    let tableId = elem.closest('.table').getAttribute('table-id');
    joinToTable(tableId);
}

function joinToTable(tableId) {
    SendRequest({
        method: 'Post',
        url: '/Home/Join',
        body: {
            tableId: tableId,
            playerSecret: user.secret,
            playerName: user.name,
        },
        success(data) {
            gameStatus = null;
            getStatus();
        }
    });
}

function leaveFromTable() {
    SendRequest({
        method: 'Post',
        url: '/Home/Leave',
        body: {
            playerSecret: user.secret
        },
        success(data) {
            getStatus();
        }
    });
}

function startGame() {
    SendRequest({
        method: 'Post',
        url: '/Home/StartGame',
        body: {
            tableId: gameStatus.table.id,
            playerSecret: user.secret,
        },
        success(data) {
            getStatus();
        }
    });
}

const gameStatusList = {
    waitPlayers: 0,
    readyToStart: 1,
    inProcess: 2,
    finish: 3,
};

const stopRoundStatus = {
    take: 0,
    successDefence: 1,
};

function getStatus() {
    if (user == null) {
        return;
    }
    SendRequest({
        method: 'Get',
        url: '/Home/GetStatus?playerSecret=' + user.secret + "&version=" + (gameStatus == null ? null : gameStatus.version),
        success(data) {
            let status = JSON.parse(data.responseText);
            if (gameStatus != null && gameStatus.version == status.version) {
                return;
            }

            gameStatus = status;
            document.getElementById('myIcon').innerHTML = "";
            document.getElementById('hand').innerHTML = ""; // todo запомнить выделенные карты, и после перерисовки выделить их снова
            document.getElementById('field').innerHTML = ""; // todo  запомнить выделенные карты, и после перерисовки выделить их снова
            document.getElementById('tables').innerHTML = "";
            document.getElementById('deckCards').innerHTML = "";
            document.getElementById('trumpInfo').innerHTML = "";
            document.getElementById('players').innerHTML = "";
            document.getElementById('takeCards').classList.add('hidden');
            document.getElementById('startGame').classList.add('hidden');

            if (status.table == null) {
                document.getElementById('createTableBtn').classList.remove('hidden');
                document.getElementById('leaveFromTableBtn').classList.add('hidden');
            } else {
                document.getElementById('createTableBtn').classList.add('hidden');
                document.getElementById('leaveFromTableBtn').classList.remove('hidden');
            }

            if (status.table == null) {
                for (let i = 0; i < status.tables.length; i++) {
                    let table = status.tables[i];
                    let tableDiv = document.getElementsByClassName('template-table')[0].cloneNode(true);
                    tableDiv.classList.remove('template-table');
                    tableDiv.setAttribute('table-id', table.id);
                    for (var j = 0; j < table.players.length; j++) {
                        let player = table.players[j];
                        let playerLabel = document.createElement("label");
                        playerLabel.innerHTML = player.name;
                        tableDiv.appendChild(playerLabel);

                    }
                    document.getElementById('tables').appendChild(tableDiv);
                }
            } else {
                if (status.table.status != gameStatusList.inProcess
                    && status.table.players.length > 0
                    && status.table.myPlayerIndex == status.table.ownerIndex) {
                    document.getElementById('startGame').classList.remove('hidden');
                }

                // draw currentGame
                for (let i = 0; i < status.table.myCards.length; i++) {
                    let card = status.table.myCards[i];
                    let cardDiv = getCardDiv(card);
                    cardDiv.addEventListener('click', function (event) {
                        if (event.target.classList.contains('active')) {
                            event.target.classList.remove('active');
                        } else {
                            event.target.classList.add('active');
                        }
                        checkMove();
                    });
                    document.getElementById('hand').appendChild(cardDiv);
                }

                let trump = status.table.trump;
                if (trump != null) {
                    if (status.table.deckCardsCount > 0) {
                        let cardDiv = getCardDiv(trump);
                        document.getElementById('deckCards').appendChild(cardDiv);
                    } else {
                        document.getElementById('trumpInfo').innerHTML = 'Козырь: ' + getSuit(trump.suit);
                    }
                }
                if (status.table.deckCardsCount > 1) {
                    let cardDiv = document.getElementsByClassName('template-card-back')[0].cloneNode(true);
                    cardDiv.classList.remove('template-card-back');
                    cardDiv.innerHTML = status.table.deckCardsCount - 1;
                    document.getElementById('deckCards').appendChild(cardDiv);
                }

                for (let i = 0; i < status.table.cards.length; i++) {
                    let fieldCardDiv = document.createElement("div");
                    fieldCardDiv.classList.add('field-card');
                    let card = status.table.cards[i];
                    let attackCard = card.attackCard;
                    let defenceCard = card.defenceCard;
                    let attackCardDiv = getCardDiv(attackCard);
                    attackCardDiv.classList.add('attack-card');
                    if (defenceCard == null) {
                        attackCardDiv.addEventListener('click', function (event) {
                            if (event.target.classList.contains('active')) {
                                event.target.classList.remove('active');
                            } else {
                                event.target.classList.add('active');
                            }
                            checkMove();
                        });
                    }

                    fieldCardDiv.appendChild(attackCardDiv);

                    if (defenceCard != null) {
                        let defenceCardDiv = getCardDiv(defenceCard);
                        defenceCardDiv.classList.add('defence-card');
                        fieldCardDiv.appendChild(defenceCardDiv);
                    }
                    document.getElementById('field').appendChild(fieldCardDiv);
                }

                let playerIndexes = [];
                for (let i = status.table.myPlayerIndex - 1; i >= 0; i--) {
                    playerIndexes.push({ index: i, gameIndex: i });
                }
                for (let i = status.table.players.length - 1; i >= status.table.myPlayerIndex; i--) {
                    playerIndexes.push({ index: i, gameIndex: i + 1 });
                }

                let defencePlayerDiv = null;
                for (let i = 0; i < playerIndexes.length; i++) {

                    let playerIndex = playerIndexes[i].index;
                    let needShowCard = status.table.needShowCardMinTrumpValue != null && playerIndexes[i].gameIndex == status.table.activePlayerIndex;
                    //я третий 2 1 5 4
                    let player = status.table.players[playerIndex];
                    let playerDiv = getPlayerDiv(playerIndexes[i].gameIndex, player.name);
                    playerIndexes[i].playerDiv = playerDiv;

                    if (playerIndexes[i].gameIndex == status.table.activePlayerIndex) {
                        playerDiv.classList.add('active-player');
                    }
                    if (playerIndexes[i].gameIndex == status.table.defencePlayerIndex) {
                        playerDiv.classList.add('defence-player');
                        defencePlayerDiv = playerDiv;
                    }
                    if (player.afkEndTime != null) {
                        let endDate = new Date(player.afkEndTime);
                        speakTimerRun(playerDiv, endDate, function (seconds) {
                            return 'осталось ' + seconds + ' секунд';
                        });
                    }

                    document.getElementById('players').appendChild(playerDiv);

                    for (var j = 0; j < player.cardsCount; j++) {
                        if (needShowCard && j == player.cardsCount - 1) {
                            let cardDiv = getCardDiv({
                                suit: status.table.trump.suit,
                                rank: status.table.needShowCardMinTrumpValue,
                            });
                            cardDiv.classList.add('show-card');
                            cardDiv.classList.remove('play-card');
                            cardDiv.classList.add('card-back');

                            playerDiv.getElementsByClassName('player-cards')[0].appendChild(cardDiv);
                        } else {
                            let cardDiv = document.getElementsByClassName('template-card-back')[0].cloneNode(true);
                            cardDiv.classList.remove('template-card-back');
                            playerDiv.getElementsByClassName('player-cards')[0].appendChild(cardDiv);
                        }
                    }
                }

                if (status.table.leavePlayer != null) {
                    let playerDiv = getPlayerDiv(-1, status.table.leavePlayer.name);
                    playerDiv.classList.add('leave-player');
                    let mySpeakDiv = getSpeakDiv();
                    mySpeakDiv.innerHTML = 'я вышел, как крыса';
                    playerDiv.prepend(mySpeakDiv);
                    for (var j = 0; j < status.table.leavePlayer.cardsCount; j++) {
                        let cardDiv = document.getElementsByClassName('template-card-back')[0].cloneNode(true);
                        cardDiv.classList.remove('template-card-back');
                        playerDiv.getElementsByClassName('player-cards')[0].appendChild(cardDiv);
                    }
                    if (status.table.leavePlayer.index == status.table.myPlayerIndex) {
                        document.getElementById('players').prepend(playerDiv);
                    } else {
                        let leaverIndex = status.table.leavePlayer.index;
                        if (leaverIndex > status.table.players.length) {
                            leaverIndex = 0;
                        }
                        if (status.table.players.length == 0) {
                            $('#players').append(playerDiv);
                        } else {
                            if (leaverIndex == status.table.myPlayerIndex) {
                                let rightPlayerDiv = document.querySelector('#players .player');
                                rightPlayerDiv.parentNode.insertBefore(playerDiv, rightPlayerDiv);
                            } else {
                                let rightPlayerDiv = document.querySelector('#players .player[data-player-index="' + leaverIndex + '"]');
                                rightPlayerDiv.after(playerDiv);
                            }
                        }
                    }
                }

                let myPlayerDiv = document.getElementsByClassName('template-player')[0].cloneNode(true);
                myPlayerDiv.classList.remove('template-player');
                myPlayerDiv.getElementsByClassName('player-name')[0].innerHTML = user.name;
                myPlayerDiv.getElementsByClassName('player-name')[0].title = user.name;
                if (status.table.myPlayerIndex == status.table.activePlayerIndex) {
                    myPlayerDiv.classList.add('active-player');
                }
                if (status.table.myPlayerIndex == status.table.defencePlayerIndex) {
                    myPlayerDiv.classList.add('defence-player');
                    defencePlayerDiv = myPlayerDiv;
                }
                document.getElementById('myIcon').appendChild(myPlayerDiv);
                if (status.table.afkEndTime != null) {
                    let endDate = new Date(status.table.afkEndTime);
                    speakTimerRun(myPlayerDiv, endDate, function (seconds) {
                        return 'осталось ' + seconds + ' секунд';
                    });
                }

                if (status.table.stopRoundStatus != null) {
                    let endDate = new Date(status.table.stopRoundEndDate);
                    speakTimerRun(defencePlayerDiv, endDate, function (seconds) {
                        if (status.table.stopRoundStatus == stopRoundStatus.take) {
                            return 'я забираю через ' + seconds + ' секунд';
                        } else if (status.table.stopRoundStatus == stopRoundStatus.successDefence) {
                            return 'отбито через ' + seconds + ' секунд';
                        } else {
                            return "что то пошло не так";
                        }
                    });
                }

                if (status.table.cards.length > 0
                    && status.table.myPlayerIndex == status.table.defencePlayerIndex) {
                    let hasNotDefencedCard = false;
                    for (let i = 0; i < status.table.cards.length; i++) {
                        if (status.table.cards[i].defenceCard == null) {
                            hasNotDefencedCard = true;
                            break;
                        }
                    }
                    if (hasNotDefencedCard) {
                        document.getElementById('takeCards').classList.remove('hidden');
                    }
                }

                checkMove();

                if (status.table.gameStatus == gameStatus.finish) {
                    let mySpeakDiv = getSpeakDiv();
                    mySpeakDiv.innerHTML = 'я дурач☻к';
                    if (status.table.myPlayerIndex == status.table.looserPlayerIndex) {
                        myPlayerDiv.prepend(mySpeakDiv);
                    } else {
                        for (let i = 0; i < playerIndexes.length; i++) {
                            if (playerIndexes[i].gameIndex == status.table.looserPlayerIndex) {
                                playerIndexes[i].playerDiv.prepend(mySpeakDiv);
                            }
                        }
                    }
                }
            }
        }
    });
}

function speakTimerRun(playerDiv, endDate, action) {

    let mySpeakDiv = getSpeakDiv();
    playerDiv.prepend(mySpeakDiv);
    if (speakTimerIntervalId != null) {
        console.log(speakTimerIntervalId);
        clearInterval(speakTimerIntervalId);
    }

    function tickTack() {
        let now = new Date();
        let diffSeconds = Math.round((endDate - now) / 1000);
        mySpeakDiv.innerHTML = action(diffSeconds);
    }

    speakTimerIntervalId = setInterval(function () {
        tickTack();
    }, 1000);
    tickTack();
}

function getCardDiv(card) {
    let cardDiv = document.getElementsByClassName('template-card')[0].cloneNode(true);
    cardDiv.classList.remove('template-card');
    cardDiv.getElementsByClassName('card-rank')[0].innerHTML = getRank(card.suit, card.rank);
    cardDiv.getElementsByClassName('card-suit')[0].innerHTML = getSuit(card.suit);
    return cardDiv;
}

function getSpeakDiv() {
    let mySpeakDiv = document.createElement("label");
    mySpeakDiv.classList.add('speak-text');
    return mySpeakDiv;
}

function getPlayerDiv(playerIndex, playerName) {
    let playerDiv = document.getElementsByClassName('template-player')[0].cloneNode(true);
    playerDiv.classList.remove('template-player');
    playerDiv.getElementsByClassName('player-name')[0].innerHTML = playerName;
    playerDiv.getElementsByClassName('player-name')[0].title = playerName;
    playerDiv.setAttribute('data-player-index', playerIndex);
    return playerDiv;
}

function canPlayCards(handCardIndexes, fieldCardIndexes) {
    let tableCardsCount = gameStatus.table.cards.length;

    let isStartAttacking = handCardIndexes.length > 0
        && gameStatus.table.activePlayerIndex === gameStatus.table.myPlayerIndex
        && tableCardsCount === 0;

    let isAttacking = handCardIndexes.length > 0
        && gameStatus.table.defencePlayerIndex !== gameStatus.table.myPlayerIndex
        && tableCardsCount > 0;

    let isDefending = handCardIndexes.length === 1
        && fieldCardIndexes.length === 1
        && gameStatus.table.defencePlayerIndex === gameStatus.table.myPlayerIndex
        && isValidDefence(fieldCardIndexes[0], handCardIndexes[0]);
    
    return isStartAttacking || isAttacking || isDefending;
}

function checkMove() {
    let handCardIndexes = getHandActiveCardIndexes();
    let fieldCardIndexes = getFieldActiveCardIndexes();
    let isShow = canPlayCards(handCardIndexes, fieldCardIndexes);

    if (isShow) {
        document.getElementById('playCards').classList.remove('hidden');
    } else {
        document.getElementById('playCards').classList.add('hidden');
    }
}

function isValidDefence(attackCardIndex, defenceCardIndex) {
    let defenceCard = gameStatus.table.myCards[defenceCardIndex];
    let attackCard = gameStatus.table.cards[attackCardIndex].attackCard;
    let trump = gameStatus.table.trump;
    if (defenceCard.suit == trump.suit) {
        if (attackCard.suit == trump.suit) {
            if (defenceCard.rank > attackCard.rank) {
                return true;
            } else {
                return false;
            }
        } else {
            return true;
        }
    } else {
        if (attackCard.suit == trump.suit) {
            return false;
        } else {
            if (attackCard.suit == defenceCard.suit) {
                if (defenceCard.rank > attackCard.rank) {
                    return true;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }
    }
}

function play() {
    let defenceCardIndexes = getHandActiveCardIndexes();
    let attackCardIndexes = getFieldActiveCardIndexes();

    if (canPlayCards(defenceCardIndexes, attackCardIndexes)) {
        SendRequest({
            method: 'Post',
            url: '/Home/PlayCards',
            body: {
                tableId: gameStatus.table.id,
                playerSecret: user.secret,
                cardIndexes: defenceCardIndexes,
                attackCardIndex: attackCardIndexes.length > 0 ? attackCardIndexes[0] : null,
            },
            success(data) {
                getStatus();
            }
        });
    }
}

function getFieldActiveCardIndexes() {
    let cards = document.querySelectorAll("#field .attack-card");
    let cardIndexes = [];
    for (let i = 0; i < cards.length; i++) {
        if (cards[i].classList.contains('active')) {
            cardIndexes.push(i);
        }
    }
    return cardIndexes;
}

function getHandActiveCardIndexes() {
    let cards = document.querySelectorAll("#hand .play-card");
    let cardIndexes = [];
    for (let i = 0; i < cards.length; i++) {
        if (cards[i].classList.contains('active')) {
            cardIndexes.push(i);
        }
    }
    return cardIndexes;
}

function take() {
    SendRequest({
        method: 'Post',
        url: '/Home/Take',
        body: {
            tableId: gameStatus.table.id,
            playerSecret: user.secret,
        },
        success(data) {
            getStatus();
        }
    });
}

function login() {
    let name = document.getElementById('nameInput').value;
    let id = uuidv4();
    setCookie(authCookieName, name, 1);
    setCookie(authCookieSecret, id, 1);
    init();
}

function logout() {
    if (gameStatus != null && gameStatus.table != null) {
        leaveFromTable();
    }
    deleteCookie(authCookieName);
    deleteCookie(authCookieSecret);
    init();
}

function setCookie(name, value, days) {
    let expires = "";
    if (days) {
        const date = new Date();
        date.setTime(date.getTime() + days * 24 * 60 * 60 * 1000);
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/";
}

function deleteCookie(name) {
    document.cookie = name + '=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;';
}

function getCookie(name) {
    const nameEQ = name + "=";
    const ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}

function uuidv4() {
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
        (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
    );
}

const ranks = {
    "6": "6",
    "7": "7",
    "8": "8",
    "9": "9",
    "10": "10",
    "11": "J",
    "12": "Q",
    "13": "K",
    "14": "A",
};

function getRank(suitValue, rankValue) {
    if (suitValue == 2 || suitValue == 1) {
        return "<label style='color:red'>" + ranks[rankValue] + "<label>";
    } else {
        return "<label>" + ranks[rankValue] + "<label>";
    }
}

const suits = {
    "0": "♣",
    "1": "♦",
    "2": "♥",
    "3": "♠",
};

function getSuit(suitValue) {
    if (suitValue == 2 || suitValue == 1) {
        return "<label style='color:red'>" + suits[suitValue] + "<label>";
    } else {
        return "<label>" + suits[suitValue] + "<label>";
    }
}


