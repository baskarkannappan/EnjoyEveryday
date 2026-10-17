// EnjoyEveryday Standalone Manual & HTML Download Utilities

window.downloadStandaloneHtml = function (fileName, htmlContent) {
    try {
        const blob = new Blob([htmlContent], { type: 'text/html;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName || 'Experience_Playbook_Manual.html';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
        console.error('Failed to download standalone HTML manual:', err);
    }
};
