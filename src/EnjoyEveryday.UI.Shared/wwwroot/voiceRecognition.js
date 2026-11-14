window.voiceRecognition = {
    recognition: null,
    dotNetHelper: null,
    isListening: false,

    isSupported: function () {
        return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
    },

    start: function (dotNetHelper, lang) {
        this.dotNetHelper = dotNetHelper;
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SpeechRecognition) {
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnSpeechError', 'Speech recognition is not supported in this browser. Fallback active: please type your observation directly below.');
            }
            return false;
        }

        try {
            if (this.recognition) {
                try { this.recognition.abort(); } catch (e) { }
            }

            this.recognition = new SpeechRecognition();
            this.recognition.continuous = true;
            this.recognition.interimResults = true;
            this.recognition.lang = lang || navigator.language || 'en-US';

            const self = this;

            this.recognition.onstart = function () {
                self.isListening = true;
                if (self.dotNetHelper) {
                    self.dotNetHelper.invokeMethodAsync('OnSpeechStarted');
                }
            };

            this.recognition.onresult = function (event) {
                let interimTranscript = '';
                let finalTranscript = '';

                for (let i = event.resultIndex; i < event.results.length; ++i) {
                    const transcript = event.results[i][0].transcript;
                    if (event.results[i].isFinal) {
                        finalTranscript += transcript;
                    } else {
                        interimTranscript += transcript;
                    }
                }

                if (self.dotNetHelper) {
                    self.dotNetHelper.invokeMethodAsync('OnSpeechResult', finalTranscript, interimTranscript);
                }
            };

            this.recognition.onerror = function (event) {
                console.warn('Speech recognition error:', event.error);
                let errorMsg = 'Speech recognition error (' + event.error + '). Text input fallback active.';
                if (event.error === 'not-allowed' || event.error === 'permission-denied') {
                    errorMsg = 'Microphone permission was denied. Fallback active: please type your observation directly below.';
                } else if (event.error === 'no-speech') {
                    errorMsg = 'No speech detected. Voice paused. You can speak again or type directly below.';
                }
                if (self.dotNetHelper) {
                    self.dotNetHelper.invokeMethodAsync('OnSpeechError', errorMsg);
                }
            };

            this.recognition.onend = function () {
                self.isListening = false;
                if (self.dotNetHelper) {
                    self.dotNetHelper.invokeMethodAsync('OnSpeechEnded');
                }
            };

            this.recognition.start();
            return true;
        } catch (err) {
            console.error('Failed to start speech recognition:', err);
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnSpeechError', 'Could not access microphone. Fallback active: please type your observation directly below.');
            }
            return false;
        }
    },

    stop: function () {
        if (this.recognition && this.isListening) {
            try {
                this.recognition.stop();
            } catch (e) { }
        }
        this.isListening = false;
    },

    playGentleChime: function () {
        try {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (!AudioContext) return;
            const ctx = new AudioContext();

            // Gentle nature harp chime (528 Hz Love/Miracle frequency & 660 Hz)
            const tones = [528, 660];
            tones.forEach((freq, index) => {
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();

                osc.type = 'sine';
                osc.frequency.setValueAtTime(freq, ctx.currentTime + index * 0.25);

                gain.gain.setValueAtTime(0, ctx.currentTime + index * 0.25);
                gain.gain.linearRampToValueAtTime(0.18, ctx.currentTime + index * 0.25 + 0.05);
                gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + index * 0.25 + 1.3);

                osc.connect(gain);
                gain.connect(ctx.destination);

                osc.start(ctx.currentTime + index * 0.25);
                osc.stop(ctx.currentTime + index * 0.25 + 1.3);
            });
        } catch (e) {
            console.warn('Audio chime playback error:', e);
        }
    }
};

