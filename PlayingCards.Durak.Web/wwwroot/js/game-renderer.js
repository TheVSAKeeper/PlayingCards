(function () {
    'use strict';

    window.renderGameState = function () {
        if (!window.gameStatus) {
            return;
        }

        const status = window.gameStatus;

        document.getElementById('myIcon').innerHTML = '';
        document.getElementById('hand').innerHTML = ''; // todo запомнить выделенные карты, и после перерисовки выделить их снова
        document.getElementById('field').innerHTML = ''; // todo  запомнить выделенные карты, и после перерисовки выделить их снова
        document.getElementById('tables').innerHTML = '';
        document.getElementById('deckCards').innerHTML = '';
        document.getElementById('trumpInfo').innerHTML = '';
        document.getElementById('players').innerHTML = '';
        document.getElementById('takeCards').classList.add('hidden');
        document.getElementById('startGame').classList.add('hidden');

        if (status.table == null) {
            document.getElementById('createTableBtn').classList.remove('hidden');
            document.getElementById('leaveFromTableBtn').classList.add('hidden');
        } else {
            document.getElementById('createTableBtn').classList.add('hidden');
            document.getElementById('leaveFromTableBtn').classList.remove('hidden');
        }

        if (status.table === null) {
            renderLobbyTables(status);
        } else {
            renderGameTable(status);
        }
    };

    function renderLobbyTables(status) {
        for (let i = 0; i < status.tables.length; i++) {
            const table = status.tables[i];
            const tableDiv = document.getElementsByClassName('template-table')[0].cloneNode(true);
            tableDiv.classList.remove('template-table');
            tableDiv.setAttribute('table-id', table.id);
            for (let j = 0; j < table.players.length; j++) {
                const player = table.players[j];
                const playerLabel = document.createElement('label');
                playerLabel.innerHTML = player.name;
                tableDiv.appendChild(playerLabel);
            }
            document.getElementById('tables').appendChild(tableDiv);
        }
    }

    function renderGameTable(status) {
        if (status.table.status !== window.gameStatusList.inProcess
            && status.table.players.length > 0
            && status.table.myPlayerIndex === status.table.ownerIndex) {
            document.getElementById('startGame').classList.remove('hidden');
        }

        renderPlayerHand(status);
        renderTrumpAndDeck(status);
        renderFieldCards(status);
        renderOtherPlayers(status);
        renderLeavingPlayer(status);
        renderCurrentPlayer(status);
        setupGameTimers(status);
        showTakeCardsButton(status);

        window.checkMove();

        showGameFinishState(status);
    }

    function renderPlayerHand(status) {
        for (let i = 0; i < status.table.myCards.length; i++) {
            const card = status.table.myCards[i];
            const cardDiv = window.getCardDiv(card);
            cardDiv.addEventListener('click', function (event) {
                if (event.target.classList.contains('active')) {
                    event.target.classList.remove('active');
                } else {
                    event.target.classList.add('active');
                }
                window.checkMove();
            });
            document.getElementById('hand').appendChild(cardDiv);
        }
    }

    function renderTrumpAndDeck(status) {
        const trump = status.table.trump;
        if (trump != null) {
            if (status.table.deckCardsCount > 0) {
                const cardDiv = window.getCardDiv(trump);
                document.getElementById('deckCards').appendChild(cardDiv);
            } else {
                document.getElementById('trumpInfo').innerHTML = 'Козырь: ' + window.getSuit(trump.suit);
            }
        }
        if (status.table.deckCardsCount > 1) {
            const cardDiv = document.getElementsByClassName('template-card-back')[0].cloneNode(true);
            cardDiv.classList.remove('template-card-back');
            cardDiv.innerHTML = status.table.deckCardsCount - 1;
            document.getElementById('deckCards').appendChild(cardDiv);
        }
    }

    function renderFieldCards(status) {
        for (let i = 0; i < status.table.cards.length; i++) {
            const fieldCardDiv = document.createElement('div');
            fieldCardDiv.classList.add('field-card');
            const card = status.table.cards[i];
            const attackCard = card.attackCard;
            const defenceCard = card.defenceCard;
            const attackCardDiv = window.getCardDiv(attackCard);
            attackCardDiv.classList.add('attack-card');
            if (defenceCard == null) {
                attackCardDiv.addEventListener('click', function (event) {
                    if (event.target.classList.contains('active')) {
                        event.target.classList.remove('active');
                    } else {
                        event.target.classList.add('active');
                    }
                    window.checkMove();
                });
            }

            fieldCardDiv.appendChild(attackCardDiv);

            if (defenceCard != null) {
                const defenceCardDiv = window.getCardDiv(defenceCard);
                defenceCardDiv.classList.add('defence-card');
                fieldCardDiv.appendChild(defenceCardDiv);
            }
            document.getElementById('field').appendChild(fieldCardDiv);
        }
    }

    function renderOtherPlayers(status) {
        const playerIndexes = [];
        for (let i = status.table.myPlayerIndex - 1; i >= 0; i--) {
            playerIndexes.push({ index: i, gameIndex: i });
        }
        for (let i = status.table.players.length - 1; i >= status.table.myPlayerIndex; i--) {
            playerIndexes.push({ index: i, gameIndex: i + 1 });
        }

        let defencePlayerDiv = null;
        for (let i = 0; i < playerIndexes.length; i++) {
            const playerIndex = playerIndexes[i].index;
            const needShowCard = status.table.needShowCardMinTrumpValue != null && playerIndexes[i].gameIndex === status.table.activePlayerIndex;
            const player = status.table.players[playerIndex];
            const playerDiv = window.getPlayerDiv(playerIndexes[i].gameIndex, player.name);
            playerIndexes[i].playerDiv = playerDiv;

            if (playerIndexes[i].gameIndex === status.table.activePlayerIndex) {
                playerDiv.classList.add('active-player');
            }
            if (playerIndexes[i].gameIndex === status.table.defencePlayerIndex) {
                playerDiv.classList.add('defence-player');
                defencePlayerDiv = playerDiv;
            }
            if (player.afkEndTime != null) {
                const endDate = new Date(player.afkEndTime);
                window.speakTimerRun(playerDiv, endDate, function (seconds) {
                    return 'осталось ' + seconds + ' секунд';
                });
            }

            document.getElementById('players').appendChild(playerDiv);

            for (let cardIndex = 0; cardIndex < player.cardsCount; cardIndex++) {
                if (needShowCard && cardIndex === player.cardsCount - 1) {
                    const cardDiv = window.getCardDiv({
                        suit: status.table.trump.suit,
                        rank: status.table.needShowCardMinTrumpValue
                    });
                    cardDiv.classList.add('show-card');
                    cardDiv.classList.remove('play-card');
                    cardDiv.classList.add('card-back');

                    playerDiv.getElementsByClassName('player-cards')[0].appendChild(cardDiv);
                } else {
                    const cardDiv = document.getElementsByClassName('template-card-back')[0].cloneNode(true);
                    cardDiv.classList.remove('template-card-back');
                    playerDiv.getElementsByClassName('player-cards')[0].appendChild(cardDiv);
                }
            }
        }

        status._defencePlayerDiv = defencePlayerDiv;
        status._playerIndexes = playerIndexes;
    }

    function renderLeavingPlayer(status) {
        if (status.table.leavePlayer != null) {
            const playerDiv = window.getPlayerDiv(-1, status.table.leavePlayer.name);
            playerDiv.classList.add('leave-player');
            const mySpeakDiv = window.getSpeakDiv();
            mySpeakDiv.innerHTML = 'я вышел, как крыса';
            playerDiv.prepend(mySpeakDiv);
            for (let k = 0; k < status.table.leavePlayer.cardsCount; k++) {
                const cardDiv = document.getElementsByClassName('template-card-back')[0].cloneNode(true);
                cardDiv.classList.remove('template-card-back');
                playerDiv.getElementsByClassName('player-cards')[0].appendChild(cardDiv);
            }
            if (status.table.leavePlayer.index === status.table.myPlayerIndex) {
                document.getElementById('players').prepend(playerDiv);
            } else {
                let leaverIndex = status.table.leavePlayer.index;
                if (leaverIndex > status.table.players.length) {
                    leaverIndex = 0;
                }
                if (status.table.players.length === 0) {
                    $('#players').append(playerDiv);
                } else {
                    if (leaverIndex === status.table.myPlayerIndex) {
                        const rightPlayerDiv = document.querySelector('#players .player');
                        rightPlayerDiv.parentNode.insertBefore(playerDiv, rightPlayerDiv);
                    } else {
                        const rightPlayerDiv = document.querySelector('#players .player[data-player-index="' + leaverIndex + '"]');
                        rightPlayerDiv.after(playerDiv);
                    }
                }
            }
        }
    }

    function renderCurrentPlayer(status) {
        const myPlayerDiv = document.getElementsByClassName('template-player')[0].cloneNode(true);
        myPlayerDiv.classList.remove('template-player');
        myPlayerDiv.getElementsByClassName('player-name')[0].innerHTML = window.user.name;
        myPlayerDiv.getElementsByClassName('player-name')[0].title = window.user.name;
        if (status.table.myPlayerIndex === status.table.activePlayerIndex) {
            myPlayerDiv.classList.add('active-player');
        }
        if (status.table.myPlayerIndex === status.table.defencePlayerIndex) {
            myPlayerDiv.classList.add('defence-player');
            status._defencePlayerDiv = myPlayerDiv;
        }
        document.getElementById('myIcon').appendChild(myPlayerDiv);
        if (status.table.afkEndTime != null) {
            const endDate = new Date(status.table.afkEndTime);
            window.speakTimerRun(myPlayerDiv, endDate, function (seconds) {
                return 'осталось ' + seconds + ' секунд';
            });
        }

        status._myPlayerDiv = myPlayerDiv;
    }

    function setupGameTimers(status) {
        if (status.table.stopRoundStatus != null && status._defencePlayerDiv) {
            const endDate = new Date(status.table.stopRoundEndDate);
            window.speakTimerRun(status._defencePlayerDiv, endDate, function (seconds) {
                if (status.table.stopRoundStatus === window.stopRoundStatus.take) {
                    return 'я забираю через ' + seconds + ' секунд';
                } else if (status.table.stopRoundStatus === window.stopRoundStatus.successDefence) {
                    return 'отбито через ' + seconds + ' секунд';
                } else {
                    return 'что то пошло не так';
                }
            });
        }
    }

    function showTakeCardsButton(status) {
        if (status.table.cards.length > 0
            && status.table.myPlayerIndex === status.table.defencePlayerIndex) {
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
    }

    function showGameFinishState(status) {
        if (status.table.gameStatus === window.gameStatusList.finish) {
            const mySpeakDiv = window.getSpeakDiv();
            mySpeakDiv.innerHTML = 'я дурач☻к';
            if (status.table.myPlayerIndex === status.table.looserPlayerIndex) {
                status._myPlayerDiv.prepend(mySpeakDiv);
            } else {
                for (let i = 0; i < status._playerIndexes.length; i++) {
                    if (status._playerIndexes[i].gameIndex === status.table.looserPlayerIndex) {
                        status._playerIndexes[i].playerDiv.prepend(mySpeakDiv);
                    }
                }
            }
        }
    }

    console.log('Game renderer module loaded');
})();
