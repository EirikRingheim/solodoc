// QR Code generation interop using qr-code-styling library
window.qrInterop = {
    /**
     * Renders a branded QR code into a container element.
     * @param {string} containerId - The ID of the DOM element to render into.
     * @param {string} data - The URL/data to encode.
     * @param {string|null} logoUrl - URL of the company logo (center of QR). Null = no logo.
     * @param {string} accentColor - Hex color for QR dots (e.g., "#4361EE").
     * @param {number} size - QR code size in pixels.
     */
    render: function (containerId, data, logoUrl, accentColor, size) {
        var container = document.getElementById(containerId);
        if (!container) return;
        container.innerHTML = '';

        var options = {
            width: size,
            height: size,
            type: 'svg',
            data: data,
            dotsOptions: {
                color: accentColor || '#1B2A4A',
                type: 'rounded'
            },
            cornersSquareOptions: {
                color: accentColor || '#1B2A4A',
                type: 'extra-rounded'
            },
            cornersDotOptions: {
                color: accentColor || '#1B2A4A',
                type: 'dot'
            },
            backgroundOptions: {
                color: '#FFFFFF'
            },
            qrOptions: {
                errorCorrectionLevel: 'H' // High error correction for logo overlay
            }
        };

        if (logoUrl) {
            options.image = logoUrl;
            options.imageOptions = {
                crossOrigin: 'anonymous',
                margin: 8,
                imageSize: 0.35,
                hideBackgroundDots: true
            };
        }

        var qrCode = new QRCodeStyling(options);
        qrCode.append(container);

        // Store reference for download
        container._qrInstance = qrCode;
    },

    /**
     * Downloads the QR code as PNG from a container.
     * @param {string} containerId - The container element ID.
     * @param {string} fileName - File name for download.
     */
    download: function (containerId, fileName) {
        var container = document.getElementById(containerId);
        if (!container || !container._qrInstance) return;
        container._qrInstance.download({
            name: fileName || 'qr-code',
            extension: 'png'
        });
    },

    /**
     * Triggers print of a specific element.
     * @param {string} elementId - The element to print.
     */
    printElement: function (elementId) {
        var element = document.getElementById(elementId);
        if (!element) return;

        var printWindow = window.open('', '_blank');
        printWindow.document.write('<html><head><title>QR-kode</title>');
        printWindow.document.write('<link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">');
        printWindow.document.write('<style>body{margin:0;display:flex;justify-content:center;align-items:center;min-height:100vh;font-family:Inter,sans-serif;}</style>');
        printWindow.document.write('</head><body>');
        printWindow.document.write(element.outerHTML);
        printWindow.document.write('</body></html>');
        printWindow.document.close();

        // Wait for fonts + images to load
        setTimeout(function () {
            printWindow.print();
            printWindow.close();
        }, 800);
    }
};
