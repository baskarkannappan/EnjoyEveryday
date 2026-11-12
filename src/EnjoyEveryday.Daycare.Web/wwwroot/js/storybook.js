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

window.downloadFile = function (fileName, contentType, content) {
    try {
        const blob = new Blob([content], { type: contentType || 'text/plain;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName || 'export.csv';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
        console.error('Failed to download file:', err);
    }
};

window.printCurrentPage = function () {
    window.print();
};
