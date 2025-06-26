(function () {
    'use strict';

    window.updateTimerDisplay = function (timerData) {
        console.log('Timer update:', timerData.Type, timerData.RemainingSeconds, 'seconds');

        try {
            switch (timerData.Type) {
                case 0: // TimerType.Afk
                    updateAfkTimer(timerData);
                    break;
                case 1: // TimerType.Round
                    updateRoundTimer(timerData);
                    break;
                case 2: // TimerType.StopRound
                    updateStopRoundTimer(timerData);
                    break;
                default:
                    console.warn('Unknown timer type:', timerData.Type);
            }
        } catch (error) {
            console.error('Error updating timer display:', error);
        }
    };

    window.updateAfkTimer = function (timerData) {
        const playerElements = document.querySelectorAll('.player');
        playerElements.forEach(playerEl => {
            const playerSecret = playerEl.getAttribute('data-player-secret');
            if (playerSecret === timerData.PlayerSecret) {
                let timerEl = playerEl.querySelector('.afk-timer');
                if (!timerEl) {
                    timerEl = document.createElement('div');
                    timerEl.className = 'afk-timer';
                    playerEl.appendChild(timerEl);
                }

                if (timerData.RemainingSeconds > 0) {
                    timerEl.textContent = `AFK: ${timerData.RemainingSeconds}s`;
                    timerEl.style.display = 'block';

                    if (timerData.RemainingSeconds <= 10) {
                        timerEl.classList.add('timer-warning');
                    } else {
                        timerEl.classList.remove('timer-warning');
                    }
                } else {
                    timerEl.style.display = 'none';
                }
            }
        });
    };

    window.updateRoundTimer = function (timerData) {
        let timerEl = document.getElementById('round-timer');
        if (!timerEl) {
            timerEl = document.createElement('div');
            timerEl.id = 'round-timer';
            timerEl.className = 'game-timer';
            document.getElementById('game-info')?.appendChild(timerEl) || document.body.appendChild(timerEl);
        }

        if (timerData.RemainingSeconds > 0) {
            timerEl.textContent = `Round: ${timerData.RemainingSeconds}s`;
            timerEl.style.display = 'block';
        } else {
            timerEl.style.display = 'none';
        }
    };

    window.updateStopRoundTimer = function (timerData) {
        let timerEl = document.getElementById('stop-round-timer');
        if (!timerEl) {
            timerEl = document.createElement('div');
            timerEl.id = 'stop-round-timer';
            timerEl.className = 'game-timer stop-round-timer';
            document.getElementById('game-info')?.appendChild(timerEl) || document.body.appendChild(timerEl);
        }

        if (timerData.RemainingSeconds > 0) {
            timerEl.textContent = `Round ending: ${timerData.RemainingSeconds}s`;
            timerEl.style.display = 'block';

            if (timerData.RemainingSeconds <= 3) {
                timerEl.classList.add('timer-critical');
            } else if (timerData.RemainingSeconds <= 5) {
                timerEl.classList.add('timer-warning');
            }
        } else {
            timerEl.style.display = 'none';
            timerEl.classList.remove('timer-warning', 'timer-critical');
        }
    };

    window.speakTimerRun = function (playerDiv, endDate, action) {
        const mySpeakDiv = window.getSpeakDiv();
        playerDiv.prepend(mySpeakDiv);

        if (window.speakTimerIntervalId != null) {
            console.log(window.speakTimerIntervalId);
            clearInterval(window.speakTimerIntervalId);
        }

        function tickTack() {
            const now = new Date();
            const diffSeconds = Math.round((endDate - now) / 1000);
            mySpeakDiv.innerHTML = action(diffSeconds);
        }

        window.speakTimerIntervalId = setInterval(function () {
            tickTack();
        }, 1000);
        tickTack();
    };

    console.log('Timer manager module loaded');
})();
