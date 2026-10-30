window.imageConverter = {
    // Cache detected MIME encoding support
    _supportedFormat: null,

    detectSupportedFormat: function () {
        if (this._supportedFormat) return this._supportedFormat;
        try {
            const canvas = document.createElement("canvas");
            canvas.width = 1;
            canvas.height = 1;
            // Check WebP encoding support
            const webp = canvas.toDataURL("image/webp");
            if (webp && webp.indexOf("data:image/webp") === 0) {
                this._supportedFormat = "image/webp";
                return "image/webp";
            }
        } catch (e) {
            // fallback
        }
        this._supportedFormat = "image/jpeg";
        return "image/jpeg";
    },

    // In-memory object URL and Blob cache for zero-base64 streams
    _blobRegistry: new Map(),

    registerBlob: function (blob) {
        const id = "blob_" + Math.random().toString(36).substring(2, 12) + "_" + Date.now();
        this._blobRegistry.set(id, blob);
        return id;
    },

    getBlobStream: function (blobId) {
        const blob = this._blobRegistry.get(blobId);
        if (!blob) return null;
        return blob.stream();
    },

    revokeBlob: function (blobId) {
        this._blobRegistry.delete(blobId);
    },

    // Calculate dimensions maintaining aspect ratio, without upscaling
    calculateBounds: function (srcWidth, srcHeight, maxWidth, maxHeight) {
        if (!srcWidth || !srcHeight) {
            return { width: maxWidth, height: maxHeight };
        }
        if (srcWidth <= maxWidth && srcHeight <= maxHeight) {
            return { width: srcWidth, height: srcHeight };
        }
        const ratio = Math.min(maxWidth / srcWidth, maxHeight / srcHeight);
        return {
            width: Math.max(1, Math.round(srcWidth * ratio)),
            height: Math.max(1, Math.round(srcHeight * ratio))
        };
    },

    // Draw image/bitmap to canvas and produce a Blob with appropriate quality
    renderToBlob: function (source, width, height, mimeType, quality) {
        return new Promise((resolve, reject) => {
            try {
                let canvas;
                if (typeof OffscreenCanvas !== "undefined") {
                    canvas = new OffscreenCanvas(width, height);
                } else {
                    canvas = document.createElement("canvas");
                    canvas.width = width;
                    canvas.height = height;
                }

                const ctx = canvas.getContext("2d", { alpha: mimeType === "image/png" });
                if (!ctx) {
                    reject(new Error("Failed to acquire 2D canvas context."));
                    return;
                }

                // Smooth resampling
                ctx.imageSmoothingEnabled = true;
                ctx.imageSmoothingQuality = "high";

                // If non-transparent, fill white background to avoid dark artifacts in JPEG
                if (mimeType === "image/jpeg") {
                    ctx.fillStyle = "#ffffff";
                    ctx.fillRect(0, 0, width, height);
                }

                ctx.drawImage(source, 0, 0, width, height);

                if (canvas.convertToBlob) {
                    canvas.convertToBlob({ type: mimeType, quality: quality })
                        .then(resolve)
                        .catch(reject);
                } else if (canvas.toBlob) {
                    canvas.toBlob((blob) => {
                        if (blob) resolve(blob);
                        else reject(new Error("Canvas toBlob failed."));
                    }, mimeType, quality);
                } else {
                    // Fallback to dataURL -> Blob
                    const dataUrl = canvas.toDataURL(mimeType, quality);
                    const binStr = atob(dataUrl.split(",")[1]);
                    const len = binStr.length;
                    const arr = new Uint8Array(len);
                    for (let i = 0; i < len; i++) {
                        arr[i] = binStr.charCodeAt(i);
                    }
                    resolve(new Blob([arr], { type: mimeType }));
                }
            } catch (err) {
                reject(err);
            }
        });
    },

    // Process a single File or Blob into dual responsive variants (thumbnail & display)
    processImageVariants: async function (fileOrBlob) {
        if (!fileOrBlob) {
            throw new Error("No file or blob provided.");
        }

        // Determine preferred format (transparent PNGs stay PNG; photos prefer WebP or JPEG)
        const format = (fileOrBlob.type === "image/png") ? "image/png" : this.detectSupportedFormat();

        // Load image safely (createImageBitmap with orientation correction, fallback to HTMLImageElement)
        let source;
        let originalWidth = 0;
        let originalHeight = 0;

        if (typeof createImageBitmap === "function") {
            try {
                // 'from-image' automatically corrects EXIF camera orientation
                source = await createImageBitmap(fileOrBlob, { imageOrientation: "from-image" });
                originalWidth = source.width;
                originalHeight = source.height;
            } catch (e) {
                // Fallback below if createImageBitmap fails
            }
        }

        if (!source) {
            source = await new Promise((resolve, reject) => {
                const img = new Image();
                const objectUrl = URL.createObjectURL(fileOrBlob);
                img.onload = () => {
                    originalWidth = img.naturalWidth || img.width;
                    originalHeight = img.naturalHeight || img.height;
                    URL.revokeObjectURL(objectUrl);
                    resolve(img);
                };
                img.onerror = (err) => {
                    URL.revokeObjectURL(objectUrl);
                    reject(new Error("Failed to decode image file."));
                };
                img.src = objectUrl;
            });
        }

        try {
            // 1. Thumbnail variant: max 320x320
            const thumbDims = this.calculateBounds(originalWidth, originalHeight, 320, 320);
            const thumbBlob = await this.renderToBlob(source, thumbDims.width, thumbDims.height, format, 0.80);

            // 2. Display variant: max 1280px on longest side
            const displayDims = this.calculateBounds(originalWidth, originalHeight, 1280, 1280);
            const displayBlob = await this.renderToBlob(source, displayDims.width, displayDims.height, format, 0.82);

            // Register blobs for streaming into Blazor without base64 overhead
            const thumbBlobId = this.registerBlob(thumbBlob);
            const displayBlobId = this.registerBlob(displayBlob);

            // Generate object URLs for immediate fast client preview
            const thumbPreviewUrl = URL.createObjectURL(thumbBlob);
            const displayPreviewUrl = URL.createObjectURL(displayBlob);

            return {
                format: format,
                originalWidth: originalWidth,
                originalHeight: originalHeight,
                thumbnail: {
                    blobId: thumbBlobId,
                    width: thumbDims.width,
                    height: thumbDims.height,
                    sizeBytes: thumbBlob.size,
                    previewUrl: thumbPreviewUrl
                },
                display: {
                    blobId: displayBlobId,
                    width: displayDims.width,
                    height: displayDims.height,
                    sizeBytes: displayBlob.size,
                    previewUrl: displayPreviewUrl
                }
            };
        } finally {
            if (source && typeof source.close === "function") {
                source.close(); // Clean up ImageBitmap
            }
        }
    },

    // Process input element files
    processInputFileElement: async function (inputFileElement) {
        if (!inputFileElement || !inputFileElement.files || inputFileElement.files.length === 0) {
            return null;
        }
        return await this.processImageVariants(inputFileElement.files[0]);
    },

    // Retrieve byte array chunk for .NET streaming transfer without full base64 strings
    getBlobBytes: async function (blobId) {
        const blob = this._blobRegistry.get(blobId);
        if (!blob) return null;
        const arrayBuffer = await blob.arrayBuffer();
        return new Uint8Array(arrayBuffer);
    },

    // --- Legacy AVIF Backward Compatibility ---
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

                    // Attempt AVIF conversion with WebP fallback if AVIF not supported
                    try {
                        const avifDataUrl = canvas.toDataURL("image/avif", 0.85);
                        if (avifDataUrl && avifDataUrl.startsWith("data:image/avif")) {
                            resolve(avifDataUrl);
                            return;
                        }
                    } catch (e) {
                        // ignore and fallback
                    }

                    // Fallback to WebP or JPEG
                    const format = window.imageConverter.detectSupportedFormat();
                    resolve(canvas.toDataURL(format, 0.85));
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
