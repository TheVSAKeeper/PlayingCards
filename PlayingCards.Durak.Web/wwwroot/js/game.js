var user = {};
var authCookieName = 'auth_name';
var authCookieSecret = 'auth_secret';

function init() {
    let cookieName = getCookie(authCookieName);
    let cookieSecret = getCookie(authCookieSecret);
    if (cookieName == null || cookieSecret == null) {
        document.getElementById('nameLabel').classList.add('hidden');
        document.getElementById('logoutBtn').classList.add('hidden');
        document.getElementById('loginBlock').classList.remove('hidden');
        document.getElementById('main').classList.add('hidden');
    } else {
        user.name = cookieName;
        user.secret = cookieSecret;
        document.getElementById('nameLabel').classList.remove('hidden');
        document.getElementById('logoutBtn').classList.remove('hidden');
        document.getElementById('loginBlock').classList.add('hidden');
        document.getElementById('main').classList.remove('hidden');
        getStatus();
    }
    if (user.name) {
        document.getElementById('nameLabel').innerHTML = user.name;
    } else {
        document.getElementById('nameLabel').innerHTML = '';
    }
}

init();

function createTable() {
    SendRequest({
        method: 'Post',
        url: '/Home/CreateTable',
        body: {
        },
        success: function (data) {
            let tableId = JSON.parse(data.responseText);
            joinToTable(tableId);
        },
        error: function (data) {
            alert('чтото пошло не так');
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
        success: function (data) {
            getStatus();
        },
        error: function (data) {
            alert('чтото пошло не так');
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
        success: function (data) {
            getStatus();
        },
        error: function (data) {
            alert('чтото пошло не так');
        }
    });
}

var gameStatus = null;
let speakTimerIntervalId = null;
function getStatus() {
    SendRequest({
        method: 'Get',
        url: '/Home/GetStatus?playerSecret=' + user.secret,
        success: function (data) {
            let status = JSON.parse(data.responseText);
            gameStatus = status;
            document.getElementById('myIcon').innerHTML = "";
            document.getElementById('hand').innerHTML = ""; // todo запомнить выделенные карты, и после перерисовки выделить их снова
            document.getElementById('field').innerHTML = ""; // todo  запомнить выделенные карты, и после перерисовки выделить их снова
            document.getElementById('tables').innerHTML = "";
            document.getElementById('deckCards').innerHTML = "";
            document.getElementById('trumpInfo').innerHTML = "";
            document.getElementById('players').innerHTML = "";



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
                    && status.table.myIndex == status.table.ownerIndex) {
                    document.getElementById('startGame').classList.remove('hidden');
                } else {
                    document.getElementById('startGame').classList.add('hidden');
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
                for (let i = status.table.myIndex - 1; i >= 0; i--) {
                    playerIndexes.push({ index: i, gameIndex: i });
                }
                for (let i = status.table.players.length - 1; i >= status.table.myIndex; i--) {
                    playerIndexes.push({ index: i, gameIndex: i + 1 });
                }

                let defencePlayerDiv = null;
                for (let i = 0; i < playerIndexes.length; i++) {

                    let playerIndex = playerIndexes[i].index;
                    //я третий 2 1 5 4
                    let player = status.table.players[playerIndex];
                    let playerDiv = document.getElementsByClassName('template-player')[0].cloneNode(true);
                    playerDiv.classList.remove('template-player');
                    playerDiv.getElementsByClassName('player-name')[0].innerHTML = player.name;
                    playerDiv.getElementsByClassName('player-name')[0].title = player.name;
                    if (playerIndexes[i].gameIndex == status.table.activePlayerIndex) {
                        playerDiv.classList.add('active-player');
                    }
                    if (playerIndexes[i].gameIndex == status.table.defencePlayerIndex) {
                        playerDiv.classList.add('defence-player');
                        defencePlayerDiv = playerDiv;
                    }
                    document.getElementById('players').appendChild(playerDiv);

                    for (var j = 0; j < player.cardsCount; j++) {
                        let cardDiv = document.getElementsByClassName('template-card-back')[0].cloneNode(true);
                        cardDiv.classList.remove('template-card-back');
                        playerDiv.getElementsByClassName('player-cards')[0].appendChild(cardDiv);
                    }
                }
                let myPlayerDiv = document.getElementsByClassName('template-player')[0].cloneNode(true);
                myPlayerDiv.classList.remove('template-player');
                myPlayerDiv.getElementsByClassName('player-name')[0].innerHTML = user.name;
                myPlayerDiv.getElementsByClassName('player-name')[0].title = user.name;
                if (status.table.myIndex == status.table.activePlayerIndex) {
                    myPlayerDiv.classList.add('active-player');
                }
                if (status.table.myIndex == status.table.defencePlayerIndex) {
                    myPlayerDiv.classList.add('defence-player');
                    defencePlayerDiv = myPlayerDiv;
                }
                document.getElementById('myIcon').appendChild(myPlayerDiv);

                if (status.table.stopRoundStatus != null) {
                    let mySpeakDiv = document.createElement("label");
                    mySpeakDiv.classList.add('speak-text');
                    let endDate = new Date(status.table.stopRoundEndDate);
                    if (speakTimerIntervalId != null) {
                        console.log(speakTimerIntervalId);
                        clearInterval(speakTimerIntervalId);
                    }
                    defencePlayerDiv.prepend(mySpeakDiv);

                    function tickTack() {
                        let now = new Date();
                        let diffSeconds = Math.round((endDate - now) / 1000, 0);
                        if (status.table.stopRoundStatus == stopRoundStatus.take) {
                            mySpeakDiv.innerHTML = 'я забираю через ' + diffSeconds + ' секунд';
                        } else if (status.table.stopRoundStatus == stopRoundStatus.successDefence) {
                            mySpeakDiv.innerHTML = 'отбито через ' + diffSeconds + ' секунд';
                        } else {
                            mySpeakDiv.innerHTML = "что то пошло не так";
                        }
                    }
                    speakTimerIntervalId = setInterval(function () {
                        tickTack();
                    }, 1000);
                    tickTack();
                }

                document.getElementById('takeCards').classList.add('hidden');
                document.getElementById('successDefenceCards').classList.add('hidden');
                if (status.table.cards.length > 0
                    && status.table.myIndex == status.table.defencePlayerIndex) {
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
                    if (!hasNotDefencedCard) {
                        document.getElementById('successDefenceCards').classList.remove('hidden');
                    }
                }
                checkMove();

            }
        },
        error: function (data) {
            alert('чтото пошло не так');
        }
    });
}

function getCardDiv(card) {
    let cardDiv = document.getElementsByClassName('template-card')[0].cloneNode(true);
    cardDiv.classList.remove('template-card');
    cardDiv.getElementsByClassName('card-rank')[0].innerHTML = getRank(card.suit, card.rank);
    cardDiv.getElementsByClassName('card-suit')[0].innerHTML = getSuit(card.suit);
    return cardDiv;
}

function checkMove() {
    let fieldCardIndexes = getFieldActiveCardIndexes();
    let handCardIndexes = getHandActiveCardIndexes();
    let tableCardsCount = gameStatus.table.cards.length;
    if (handCardIndexes.length > 0
        && gameStatus.table.activePlayerIndex == gameStatus.table.myIndex
        && tableCardsCount == 0) {
        document.getElementById('startattackCards').classList.remove('hidden');
    } else {
        document.getElementById('startattackCards').classList.add('hidden');
    }

    if (handCardIndexes.length > 0
        && gameStatus.table.activePlayerIndex == gameStatus.table.myIndex
        && tableCardsCount > 0) {
        document.getElementById('attackCards').classList.remove('hidden');
    } else {
        document.getElementById('attackCards').classList.add('hidden');
    }

    if (handCardIndexes.length == 1
        && fieldCardIndexes.length == 1
        && gameStatus.table.defencePlayerIndex == gameStatus.table.myIndex
        && isValidDefence(fieldCardIndexes[0], handCardIndexes[0])) {
        document.getElementById('defenceCards').classList.remove('hidden');
    } else {
        document.getElementById('defenceCards').classList.add('hidden');
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
            }
            else {
                return false;
            }
        }
        else {
            return true;
        }
    }
    else {
        if (attackCard.suit == trump.suit) {
            return false;
        }
        else {
            if (attackCard.suit == defenceCard.suit) {
                if (defenceCard.rank > attackCard.rank) {
                    return true;
                }
                else {
                    return false;
                }
            }
            else {
                return false;
            }
        }
    }
}

function startAttack() {
    let cardIndexes = getHandActiveCardIndexes();
    if (cardIndexes.length > 0) {
        SendRequest({
            method: 'Post',
            url: '/Home/StartAttack',
            body: {
                tableId: gameStatus.table.id,
                playerSecret: user.secret,
                cardIndexes: cardIndexes,
            },
            success: function (data) {
                getStatus();
            },
            error: function (data) {
                alert('чтото пошло не так');
            }
        });
    }
}


function attack() {
    let cardIndexes = getHandActiveCardIndexes();
    if (cardIndexes.length > 0) {
        SendRequest({
            method: 'Post',
            url: '/Home/Attack',
            body: {
                tableId: gameStatus.table.id,
                playerSecret: user.secret,
                cardIndexes: cardIndexes,
            },
            success: function (data) {
                getStatus();
            },
            error: function (data) {
                alert('чтото пошло не так');
            }
        });
    }
}

function defence() {
    let attackCardIndexes = getFieldActiveCardIndexes();
    let defenceCardIndexes = getHandActiveCardIndexes();
    if (attackCardIndexes.length == 1 && defenceCardIndexes.length == 1) {
        SendRequest({
            method: 'Post',
            url: '/Home/Defence',
            body: {
                tableId: gameStatus.table.id,
                playerSecret: user.secret,
                defenceCardIndex: defenceCardIndexes[0],
                attackCardIndex: attackCardIndexes[0],
            },
            success: function (data) {
                getStatus();
            },
            error: function (data) {
                alert('чтото пошло не так');
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
        success: function (data) {
            getStatus();
        },
        error: function (data) {
            alert('чтото пошло не так');
        }
    });
}

function successDefence() {
    SendRequest({
        method: 'Post',
        url: '/Home/SuccessDefence',
        body: {
            tableId: gameStatus.table.id,
            playerSecret: user.secret,
        },
        success: function (data) {
            getStatus();
        },
        error: function (data) {
            alert('чтото пошло не так');
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
    deleteCookie(authCookieName);
    deleteCookie(authCookieSecret);
    init();
}

function setCookie(name, value, days) {
    var expires = "";
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/";
}
function deleteCookie(name) {
    document.cookie = name + '=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;';
}

function getCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
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

function getRank(suitValue, value) {
    if (suitValue == 2 || suitValue == 1) {
        return "<label style='color:red'>" + ranks[value] + "<label>";
    } else {
        return "<label>" + ranks[value] + "<label>";
    }
    return 1;
}

function getSuit(value) {
    if (value == 2 || value == 1) {
        return "<label style='color:red'>" + suits[value] + "<label>";
    } else {
        return "<label>" + suits[value] + "<label>";
    }
}

var ranks = {
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

var suits = {
    "0": "♣",
    "1": "♦",
    "2": "♥",
    "3": "♠",
}

var gameStatusList = {
    waitPlayers: 0,
    readyToStart: 1,
    inProcess: 2,
    finish: 3,
}

var stopRoundStatus = {
    take: 0,
    successDefence: 1,
}
