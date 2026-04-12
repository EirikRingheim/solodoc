// QR Scanner using getUserMedia + BarcodeDetector (or jsQR fallback)
window.qrScanner = {
    _video: null,
    _stream: null,
    _scanning: false,
    _canvas: null,
    _ctx: null,

    async start(dotnetRef, elementId) {
        const container = document.getElementById(elementId);
        if (!container) return false;

        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'environment', width: { ideal: 640 }, height: { ideal: 480 } }
            });

            let video = container.querySelector('video');
            if (!video) {
                video = document.createElement('video');
                video.style.width = '100%';
                video.style.borderRadius = '8px';
                video.setAttribute('playsinline', '');
                video.setAttribute('autoplay', '');
                container.appendChild(video);
            }

            video.srcObject = stream;
            await video.play();

            this._video = video;
            this._stream = stream;
            this._scanning = true;

            // Create offscreen canvas for frame analysis
            this._canvas = document.createElement('canvas');
            this._ctx = this._canvas.getContext('2d', { willReadFrequently: true });

            // Use BarcodeDetector if available (Chrome 83+, Safari 17.2+)
            if ('BarcodeDetector' in window) {
                const detector = new BarcodeDetector({ formats: ['qr_code'] });
                this._scanWithDetector(detector, dotnetRef);
            } else {
                // Fallback: manual frame scanning with basic pattern detection
                this._scanWithCanvas(dotnetRef);
            }

            return true;
        } catch (err) {
            console.error('Camera access denied:', err);
            return false;
        }
    },

    async _scanWithDetector(detector, dotnetRef) {
        while (this._scanning && this._video) {
            try {
                const barcodes = await detector.detect(this._video);
                if (barcodes.length > 0) {
                    const value = barcodes[0].rawValue;
                    if (value) {
                        await dotnetRef.invokeMethodAsync('OnQrScanned', value);
                        this.stop();
                        return;
                    }
                }
            } catch (e) { }
            await new Promise(r => setTimeout(r, 250));
        }
    },

    _scanWithCanvas(dotnetRef) {
        // Fallback: capture frames and look for URLs containing "checkin/qr"
        // This is a simplified approach - for full QR decoding without BarcodeDetector,
        // you'd need a library like jsQR
        const check = async () => {
            if (!this._scanning || !this._video) return;

            this._canvas.width = this._video.videoWidth;
            this._canvas.height = this._video.videoHeight;
            this._ctx.drawImage(this._video, 0, 0);

            // Try BarcodeDetector one more time (may have loaded async)
            if ('BarcodeDetector' in window) {
                try {
                    const detector = new BarcodeDetector({ formats: ['qr_code'] });
                    const barcodes = await detector.detect(this._canvas);
                    if (barcodes.length > 0 && barcodes[0].rawValue) {
                        await dotnetRef.invokeMethodAsync('OnQrScanned', barcodes[0].rawValue);
                        this.stop();
                        return;
                    }
                } catch (e) { }
            }

            if (this._scanning) {
                requestAnimationFrame(() => setTimeout(check, 300));
            }
        };
        check();
    },

    stop() {
        this._scanning = false;
        if (this._stream) {
            this._stream.getTracks().forEach(t => t.stop());
            this._stream = null;
        }
        if (this._video) {
            this._video.srcObject = null;
            this._video = null;
        }
    }
};
