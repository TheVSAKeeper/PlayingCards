(function () {
    'use strict';

    window.getCardDiv = function (card) {
        const cardDiv = document.getElementsByClassName('template-card')[0].cloneNode(true);
        cardDiv.classList.remove('template-card');
        cardDiv.getElementsByClassName('card-rank')[0].innerHTML = getRank(card.suit, card.rank);
        cardDiv.getElementsByClassName('card-suit')[0].innerHTML = getSuit(card.suit);
        return cardDiv;
    };

    window.getRank = function (suitValue, rankValue) {
        if (suitValue === 2 || suitValue === 1) {
            return '<label style=\'color:red\'>' + window.ranks[rankValue] + '<label>';
        } else {
            return '<label>' + window.ranks[rankValue] + '<label>';
        }
    };

    window.getSuit = function (suitValue) {
        if (suitValue === 2 || suitValue === 1) {
            return '<label style=\'color:red\'>' + window.suits[suitValue] + '<label>';
        } else {
            return '<label>' + window.suits[suitValue] + '<label>';
        }
    };

    window.getSpeakDiv = function () {
        const mySpeakDiv = document.createElement('label');
        mySpeakDiv.classList.add('speak-text');
        return mySpeakDiv;
    };

    window.getPlayerDiv = function (playerIndex, playerName) {
        const playerDiv = document.getElementsByClassName('template-player')[0].cloneNode(true);
        playerDiv.classList.remove('template-player');
        playerDiv.getElementsByClassName('player-name')[0].innerHTML = playerName;
        playerDiv.getElementsByClassName('player-name')[0].title = playerName;
        playerDiv.setAttribute('data-player-index', playerIndex);
        return playerDiv;
    };

    window.canPlayCards = function (handCardIndexes, fieldCardIndexes) {
        const tableCardsCount = window.gameStatus.table.cards.length;

        const isStartAttacking = handCardIndexes.length > 0
            && window.gameStatus.table.activePlayerIndex === window.gameStatus.table.myPlayerIndex
            && tableCardsCount === 0;

        const isAttacking = handCardIndexes.length > 0
            && window.gameStatus.table.defencePlayerIndex !== window.gameStatus.table.myPlayerIndex
            && tableCardsCount > 0;

        const isDefending = handCardIndexes.length === 1
            && fieldCardIndexes.length === 1
            && window.gameStatus.table.defencePlayerIndex === window.gameStatus.table.myPlayerIndex
            && isValidDefence(fieldCardIndexes[0], handCardIndexes[0]);

        return isStartAttacking || isAttacking || isDefending;
    };

    window.isValidDefence = function (attackCardIndex, defenceCardIndex) {
        const defenceCard = window.gameStatus.table.myCards[defenceCardIndex];
        const attackCard = window.gameStatus.table.cards[attackCardIndex].attackCard;
        const trump = window.gameStatus.table.trump;

        if (defenceCard.suit === trump.suit) {
            if (attackCard.suit === trump.suit) {
                if (defenceCard.rank > attackCard.rank) {
                    return true;
                } else {
                    return false;
                }
            } else {
                return true;
            }
        } else {
            if (attackCard.suit === trump.suit) {
                return false;
            } else {
                if (attackCard.suit === defenceCard.suit) {
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
    };

    window.getFieldActiveCardIndexes = function () {
        const cards = document.querySelectorAll('#field .attack-card');
        const cardIndexes = [];
        for (let i = 0; i < cards.length; i++) {
            if (cards[i].classList.contains('active')) {
                cardIndexes.push(i);
            }
        }
        return cardIndexes;
    };

    window.getHandActiveCardIndexes = function () {
        const cards = document.querySelectorAll('#hand .play-card');
        const cardIndexes = [];
        for (let i = 0; i < cards.length; i++) {
            if (cards[i].classList.contains('active')) {
                cardIndexes.push(i);
            }
        }
        return cardIndexes;
    };

    window.checkMove = function () {
        const handCardIndexes = getHandActiveCardIndexes();
        const fieldCardIndexes = getFieldActiveCardIndexes();
        const isShow = canPlayCards(handCardIndexes, fieldCardIndexes);

        if (isShow) {
            document.getElementById('playCards').classList.remove('hidden');
        } else {
            document.getElementById('playCards').classList.add('hidden');
        }
    };

    console.log('Card utilities module loaded');
})();
