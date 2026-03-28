// SafeRoute SOS System
var sosTimer = null;
var sosCountdownValue = 5;
var currentSosAlertId = null;
var currentTrackingToken = null;

function triggerSOS() {
    var modal = new bootstrap.Modal(document.getElementById('sosModal'));
    document.getElementById('sosCountdown').style.display = 'block';
    document.getElementById('sosActive').style.display = 'none';
    sosCountdownValue = 5;
    document.getElementById('sosTimer').textContent = sosCountdownValue;
    document.getElementById('sosTimerText').textContent = sosCountdownValue;
    modal.show();

    // Start countdown
    sosTimer = setInterval(function () {
        sosCountdownValue--;
        document.getElementById('sosTimer').textContent = sosCountdownValue;
        document.getElementById('sosTimerText').textContent = sosCountdownValue;
        if (sosCountdownValue <= 0) {
            clearInterval(sosTimer);
            executeSOS();
        }
    }, 1000);
}

function cancelSOS() {
    clearInterval(sosTimer);
    bootstrap.Modal.getInstance(document.getElementById('sosModal')).hide();
}

function executeSOS() {
    document.getElementById('sosCountdown').style.display = 'none';
    document.getElementById('sosActive').style.display = 'block';

    // Get current location
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(function (pos) {
            sendSOSToServer(pos.coords.latitude, pos.coords.longitude);
        }, function () {
            // Fallback: send without location
            sendSOSToServer(23.8103, 90.4125); // Dhaka default
        });
    } else {
        sendSOSToServer(23.8103, 90.4125);
    }

    // Play alarm sound
    playAlarm();
}

function sendSOSToServer(lat, lng) {
    fetch('/api/Sos/trigger', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ latitude: lat, longitude: lng, triggerMethod: 'Button' })
    })
    .then(r => r.json())
    .then(data => {
        currentSosAlertId = data.alertId;
        currentTrackingToken = data.trackingToken;

        // Build SMS message
        var mapsLink = 'https://maps.google.com/maps?q=' + lat + ',' + lng;
        var trackLink = window.location.origin + data.trackingUrl;
        var msg = '🚨 EMERGENCY! ' + data.userName + ' needs help!\n📍 Location: ' + mapsLink + '\n🔗 Live Track: ' + trackLink + '\n— SafeRoute';

        // Open SMS to all trusted contacts
        if (data.contacts && data.contacts.length > 0) {
            var phones = data.contacts.map(c => c.phone).join(',');
            var smsLink = 'sms:' + phones + '?body=' + encodeURIComponent(msg);
            window.open(smsLink, '_self');
            document.getElementById('sosMessage').textContent =
                'SMS prepared for ' + data.contacts.length + ' trusted contacts. Live tracking active!';
        } else {
            document.getElementById('sosMessage').textContent =
                'No trusted contacts found. Add them in Profile → Trusted Contacts.';
        }

        // Start location updates every 30 seconds
        if (currentTrackingToken) {
            startLocationUpdates();
        }
    })
    .catch(() => {
        // Offline fallback: open tel: directly
        document.getElementById('sosMessage').textContent = 'Server unavailable. Call 999 directly!';
    });
}

function resolveSOS() {
    if (currentSosAlertId) {
        fetch('/api/Sos/resolve/' + currentSosAlertId, { method: 'POST' }).catch(() => {});
    }
    stopAlarm();
    stopLocationUpdates();
    bootstrap.Modal.getInstance(document.getElementById('sosModal')).hide();
}

// Location updates for live tracking
var locationInterval = null;

function startLocationUpdates() {
    locationInterval = setInterval(function () {
        if (navigator.geolocation && currentTrackingToken) {
            navigator.geolocation.getCurrentPosition(function (pos) {
                fetch('/api/Sos/location', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        token: currentTrackingToken,
                        latitude: pos.coords.latitude,
                        longitude: pos.coords.longitude
                    })
                }).catch(() => {});
            });
        }
    }, 30000);
}

function stopLocationUpdates() {
    if (locationInterval) clearInterval(locationInterval);
}

// Alarm sound using Web Audio API
var alarmOscillator = null;
var alarmContext = null;

function playAlarm() {
    try {
        alarmContext = new (window.AudioContext || window.webkitAudioContext)();
        alarmOscillator = alarmContext.createOscillator();
        var gainNode = alarmContext.createGain();
        alarmOscillator.connect(gainNode);
        gainNode.connect(alarmContext.destination);
        alarmOscillator.type = 'square';
        alarmOscillator.frequency.value = 800;
        gainNode.gain.value = 0.3;
        alarmOscillator.start();

        // Siren effect
        var up = true;
        setInterval(function () {
            if (alarmOscillator) {
                alarmOscillator.frequency.value = up ? 1200 : 600;
                up = !up;
            }
        }, 500);
    } catch (e) { }
}

function stopAlarm() {
    try {
        if (alarmOscillator) { alarmOscillator.stop(); alarmOscillator = null; }
        if (alarmContext) { alarmContext.close(); alarmContext = null; }
    } catch (e) { }
}

// Phone Shake Detection
var shakeThreshold = 20;
var shakeCount = 0;
var lastShakeTime = 0;

if (window.DeviceMotionEvent) {
    window.addEventListener('devicemotion', function (e) {
        var acc = e.accelerationIncludingGravity;
        if (!acc) return;
        var total = Math.abs(acc.x) + Math.abs(acc.y) + Math.abs(acc.z);
        if (total > 40) {
            var now = Date.now();
            if (now - lastShakeTime < 1000) {
                shakeCount++;
                if (shakeCount >= 3) {
                    shakeCount = 0;
                    triggerSOS();
                }
            } else {
                shakeCount = 1;
            }
            lastShakeTime = now;
        }
    });
}
