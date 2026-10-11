window.imageConverter = {
    convertToAvif: function (inputFileElement) {
        if (!inputFileElement || !inputFileElement.files || inputFileElement.files.length === 0) {
            return Promise.resolve(null);
        }
        return window.imageConverter.convertFileToAvif(inputFileElement.files[0]);
    },

    convertFileToAvif: function (file) {
        return new Promise((resolve, reject) => {
            if (!file) {
                resolve(null);
                return;
            }

            const reader = new FileReader();
            reader.onload = function (event) {
                const img = new Image();
                img.onload = function () {
                    const canvas = document.createElement("canvas");
                    canvas.width = img.width;
                    canvas.height = img.height;
                    
                    const ctx = canvas.getContext("2d");
                    ctx.drawImage(img, 0, 0);

                    // Convert to AVIF format with quality 0.85
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
    },

    convertMultipleToAvif: async function (inputFileElement, maxFiles = 3) {
        if (!inputFileElement || !inputFileElement.files || inputFileElement.files.length === 0) {
            return [];
        }

        const count = Math.min(inputFileElement.files.length, maxFiles);
        const results = [];
        for (let i = 0; i < count; i++) {
            try {
                const avif = await window.imageConverter.convertFileToAvif(inputFileElement.files[i]);
                if (avif) {
                    results.push(avif);
                }
            } catch (err) {
                console.error("Failed to convert image:", err);
            }
        }
        return results;
    }
};
