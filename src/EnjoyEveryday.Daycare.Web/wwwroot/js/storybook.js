// EnjoyEveryday Interactive Storybook Audio & Export Utilities

let currentUtterance = null;

window.playStoryAudio = function (text, rate, dotNetHelper) {
    if (!('speechSynthesis' in window)) {
        console.warn('Speech synthesis not supported in this browser.');
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnNarrationFinished');
        }
        return;
    }

    window.speechSynthesis.cancel();

    currentUtterance = new SpeechSynthesisUtterance(text);
    currentUtterance.rate = rate || 1.0;
    currentUtterance.pitch = 1.05; // Friendly warm pitch

    // Select the warmest, most natural voice available
    const voices = window.speechSynthesis.getVoices();
    const preferredVoice = voices.find(v => v.lang.startsWith('en') && (
        v.name.includes('Natural') ||
        v.name.includes('Samantha') ||
        v.name.includes('Google') ||
        v.name.includes('Karen') ||
        v.name.includes('Zira')
    )) || voices.find(v => v.lang.startsWith('en'));

    if (preferredVoice) {
        currentUtterance.voice = preferredVoice;
    }

    currentUtterance.onend = function () {
        currentUtterance = null;
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnNarrationFinished');
        }
    };

    currentUtterance.onerror = function (e) {
        console.warn('Speech synthesis utterance error:', e);
        currentUtterance = null;
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnNarrationFinished');
        }
    };

    window.speechSynthesis.speak(currentUtterance);
};

window.stopStoryAudio = function () {
    if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
    }
    currentUtterance = null;
};

window.downloadStandaloneHtml = function (fileName, htmlContent) {
    try {
        const blob = new Blob([htmlContent], { type: 'text/html;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName || 'Experience_Storybook.html';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
        console.error('Failed to download standalone HTML:', err);
    }
};
