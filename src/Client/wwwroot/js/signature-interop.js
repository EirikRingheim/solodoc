// Signature pad using HTML Canvas
window.signaturePad = {
    _canvas: null,
    _ctx: null,
    _drawing: false,
    _lastX: 0,
    _lastY: 0,
    _isEmpty: true,

    init(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return false;

        this._canvas = canvas;
        this._ctx = canvas.getContext('2d');
        this._isEmpty = true;

        // Set canvas size to match container
        const rect = canvas.parentElement.getBoundingClientRect();
        canvas.width = rect.width;
        canvas.height = 200;

        // Style
        this._ctx.strokeStyle = '#1a1a1a';
        this._ctx.lineWidth = 2.5;
        this._ctx.lineCap = 'round';
        this._ctx.lineJoin = 'round';

        // White background
        this._ctx.fillStyle = '#ffffff';
        this._ctx.fillRect(0, 0, canvas.width, canvas.height);

        // Events — support both mouse and touch
        const getPos = (e) => {
            const rect = canvas.getBoundingClientRect();
            if (e.touches) {
                return { x: e.touches[0].clientX - rect.left, y: e.touches[0].clientY - rect.top };
            }
            return { x: e.clientX - rect.left, y: e.clientY - rect.top };
        };

        const startDraw = (e) => {
            e.preventDefault();
            this._drawing = true;
            const pos = getPos(e);
            this._lastX = pos.x;
            this._lastY = pos.y;
            this._isEmpty = false;
        };

        const draw = (e) => {
            if (!this._drawing) return;
            e.preventDefault();
            const pos = getPos(e);
            this._ctx.beginPath();
            this._ctx.moveTo(this._lastX, this._lastY);
            this._ctx.lineTo(pos.x, pos.y);
            this._ctx.stroke();
            this._lastX = pos.x;
            this._lastY = pos.y;
        };

        const endDraw = (e) => {
            this._drawing = false;
        };

        canvas.addEventListener('mousedown', startDraw);
        canvas.addEventListener('mousemove', draw);
        canvas.addEventListener('mouseup', endDraw);
        canvas.addEventListener('mouseleave', endDraw);
        canvas.addEventListener('touchstart', startDraw, { passive: false });
        canvas.addEventListener('touchmove', draw, { passive: false });
        canvas.addEventListener('touchend', endDraw);

        return true;
    },

    clear() {
        if (!this._canvas || !this._ctx) return;
        this._ctx.fillStyle = '#ffffff';
        this._ctx.fillRect(0, 0, this._canvas.width, this._canvas.height);
        this._isEmpty = true;
    },

    isEmpty() {
        return this._isEmpty;
    },

    toDataUrl() {
        if (!this._canvas || this._isEmpty) return null;
        return this._canvas.toDataURL('image/png');
    }
};
