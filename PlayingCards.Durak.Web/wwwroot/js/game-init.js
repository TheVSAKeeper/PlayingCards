(function () {
    'use strict';

    window.init = function () {
        window.initializeAuth();

        if (window.user !== null) {
            window.initializeCommunication();
        }
    };

    window.init();

    console.log('Game initialization module loaded');
})();
