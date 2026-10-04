window.imageConverter = {
    convertToAvif: function (inputFileElement) {
        return new Promise((resolve, reject) => {
            if (!inputFileElement || !inputFileElement.files || inputFileElement.files.length === 0) {
                resolve(null);
                return;
            }

            const file = inputFileElement.files[0];
            const reader = new FileReader();

            reader.onload = function (event) {
                const img = new Image();
                img.onload = function () {
                    const canvas = document.createElement("canvas");
                    canvas.width = img.width;
                    canvas.height = img.height;
                    
                    const ctx = canvas.getContext("2d");
                    ctx.drawImage(img, 0, 0);

                    // Convert to AVIF format. 
                    // Browsers that do not support 'image/avif' will fallback to 'image/png' natively.
                    const dataUrl = canvas.toDataURL("image/avif", 0.85);
                    resolve(dataUrl);
                };
                img.onerror = function (err) {
                    reject(err);
                };
                img.src = event.target.result;
            };

            reader.onerror = function (err) {
                reject(err);
            };

            reader.readAsDataURL(file);
        });
    }
};
