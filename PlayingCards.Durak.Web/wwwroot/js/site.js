window.SendRequest = function (options) {
    const _this = {};
    const defaultOptions = {
        method: 'POST'
    };

    _this.options = Object.assign({}, defaultOptions, options);

    _this.Send = function () {
        const xhr = new XMLHttpRequest();
        xhr.open(_this.options.method, _this.options.url, true);
        xhr.setRequestHeader('Content-Type', 'application/json;charset=UTF-8');
        xhr.onreadystatechange = function () {
            if (this.readyState !== 4) {
                return;
            }
            if (this.status === 200) {
                if (_this.options.success) {
                    let responseData = null;
                    try {
                        if (this.responseText) {
                            responseData = JSON.parse(this.responseText);
                        }
                    } catch (e) {
                        responseData = this.responseText;
                    }
                    _this.options.success(responseData);
                }
            } else if (this.status === 403) {
                window.showAlert('Внимание', this.responseText, 5);
            } else {
                if (_this.options.error) {
                    _this.options.error(this);
                } else {
                    window.showAlert('Ошибка', 'чтото пошло не так', 2);
                }
            }
            if (_this.options.always) {
                _this.options.always(this);
            }
        };

        xhr.send(JSON.stringify(_this.options.body));
    };
    _this.Send();
};

let globalAlertId = 0;

window.showAlert = function (title, message, timeoutSeconds) {
    globalAlertId++;
    const alertId = globalAlertId;
    if (!document.getElementById('userAlertsBody')) {
        const alertsBody = document.createElement('div');
        alertsBody.id = 'userAlertsBody';
        alertsBody.classList.add('alerts-body');
        document.body.append(alertsBody);
    }

    const alert = document.createElement('div');
    alert.id = 'alert-' + alertId;
    alert.classList.add('alert');
    alert.classList.add('alert-warning');
    document.getElementById('userAlertsBody').append(alert);
    alert.innerHTML =
        "    <a class='close' href='#' onclick='hideAlert(this)'>X</a>"
        + '    <h4></span>' + title + '</h4>'
        + '    <label>' + message + '</label>';

    if (timeoutSeconds) {
        setTimeout(function () {
            window.hideAlertById(alert.id);
        }, timeoutSeconds * 1000);
    }
};

window.hideAlertById = function (alertId) {
    document.getElementById(alertId).remove();
};

window.hideAlert = function (elem) {
    const alertBlock = elem.closest('.alert');
    alertBlock.remove();
};

console.log('site.js loaded successfully. SendRequest function:', typeof window.SendRequest);
