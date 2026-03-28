// ─────────────────────────────────────────────────────────────────────────────
// Drawing Canvas – Pointer-Events-based drawing with pressure sensitivity
// Used by DrinkNotes.razor for handwritten drink notes on iPad + Apple Pencil
// ─────────────────────────────────────────────────────────────────────────────

window.drawingCanvas = (function () {
    let canvas = null;
    let ctx = null;
    let dotNetRef = null;
    let isDrawing = false;
    let currentStroke = null;
    let strokes = [];
    let currentColor = '#000000';
    let currentWidth = 2;
    let lineSpacing = 32; // px between ruled lines

    // ── Public API ──────────────────────────────────────────────────────────

    function initCanvas(canvasId, blazorRef) {
        canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error('[drawingCanvas] Canvas not found:', canvasId);
            return;
        }
        ctx = canvas.getContext('2d');
        dotNetRef = blazorRef;
        strokes = [];
        isDrawing = false;

        // Prevent default touch actions (scroll, zoom) on the canvas
        canvas.style.touchAction = 'none';

        // Attach pointer events
        canvas.addEventListener('pointerdown', onPointerDown);
        canvas.addEventListener('pointermove', onPointerMove);
        canvas.addEventListener('pointerup', onPointerUp);
        canvas.addEventListener('pointercancel', onPointerUp);
        canvas.addEventListener('pointerleave', onPointerUp);

        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);

        redraw();
    }

    function disposeCanvas() {
        if (canvas) {
            canvas.removeEventListener('pointerdown', onPointerDown);
            canvas.removeEventListener('pointermove', onPointerMove);
            canvas.removeEventListener('pointerup', onPointerUp);
            canvas.removeEventListener('pointercancel', onPointerUp);
            canvas.removeEventListener('pointerleave', onPointerUp);
        }
        window.removeEventListener('resize', resizeCanvas);
        canvas = null;
        ctx = null;
        dotNetRef = null;
        strokes = [];
    }

    function clearCanvas() {
        strokes = [];
        redraw();
    }

    function undoStroke() {
        if (strokes.length > 0) {
            strokes.pop();
            redraw();
        }
    }

    function setColor(color) {
        currentColor = color;
    }

    function setLineWidth(width) {
        currentWidth = width;
    }

    function getStrokeData() {
        return JSON.stringify(strokes);
    }

    function loadStrokeData(json) {
        try {
            strokes = JSON.parse(json) || [];
        } catch (e) {
            console.warn('[drawingCanvas] Invalid stroke data:', e);
            strokes = [];
        }
        redraw();
    }

    function getStrokeCount() {
        return strokes.length;
    }

    // ── Canvas sizing ───────────────────────────────────────────────────────

    function resizeCanvas() {
        if (!canvas) return;
        const parent = canvas.parentElement;
        if (!parent) return;

        const dpr = window.devicePixelRatio || 1;
        const rect = parent.getBoundingClientRect();

        canvas.width = rect.width * dpr;
        canvas.height = rect.height * dpr;
        canvas.style.width = rect.width + 'px';
        canvas.style.height = rect.height + 'px';

        ctx.scale(dpr, dpr);
        redraw();
    }

    // ── Drawing logic ───────────────────────────────────────────────────────

    function onPointerDown(e) {
        // Only respond to pen and touch; also allow mouse for desktop testing
        isDrawing = true;
        canvas.setPointerCapture(e.pointerId);

        const point = getPoint(e);
        currentStroke = {
            points: [point],
            color: currentColor,
            width: currentWidth
        };
    }

    function onPointerMove(e) {
        if (!isDrawing || !currentStroke) return;

        const point = getPoint(e);
        currentStroke.points.push(point);

        // Draw the latest segment immediately for responsiveness
        const pts = currentStroke.points;
        if (pts.length >= 2) {
            drawSegment(
                pts[pts.length - 2],
                pts[pts.length - 1],
                currentStroke.color,
                currentStroke.width
            );
        }
    }

    function onPointerUp(e) {
        if (!isDrawing) return;
        isDrawing = false;

        if (currentStroke && currentStroke.points.length > 0) {
            strokes.push(currentStroke);

            // Notify Blazor of change (for auto-save)
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnCanvasChanged')
                    .catch(err => console.warn('[drawingCanvas] OnCanvasChanged failed:', err));
            }
        }
        currentStroke = null;
    }

    function getPoint(e) {
        const rect = canvas.getBoundingClientRect();
        return {
            x: e.clientX - rect.left,
            y: e.clientY - rect.top,
            p: e.pressure || 0.5 // Apple Pencil pressure, default 0.5 for mouse
        };
    }

    // ── Rendering ───────────────────────────────────────────────────────────

    function redraw() {
        if (!ctx || !canvas) return;
        const dpr = window.devicePixelRatio || 1;
        const w = canvas.width / dpr;
        const h = canvas.height / dpr;

        // Clear
        ctx.clearRect(0, 0, w, h);

        // Draw ruled lines
        drawRuledLines(w, h);

        // Draw all strokes
        for (const stroke of strokes) {
            drawStroke(stroke);
        }
    }

    function drawRuledLines(w, h) {
        ctx.save();
        ctx.strokeStyle = '#d0d0d0';
        ctx.lineWidth = 0.5;
        for (let y = lineSpacing; y < h; y += lineSpacing) {
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(w, y);
            ctx.stroke();
        }
        ctx.restore();
    }

    function drawStroke(stroke) {
        const pts = stroke.points;
        if (pts.length < 2) {
            // Single dot
            if (pts.length === 1) {
                ctx.beginPath();
                const r = (stroke.width * pts[0].p) / 2;
                ctx.arc(pts[0].x, pts[0].y, Math.max(r, 0.5), 0, Math.PI * 2);
                ctx.fillStyle = stroke.color;
                ctx.fill();
            }
            return;
        }

        for (let i = 1; i < pts.length; i++) {
            drawSegment(pts[i - 1], pts[i], stroke.color, stroke.width);
        }
    }

    function drawSegment(p1, p2, color, baseWidth) {
        ctx.beginPath();
        ctx.strokeStyle = color;
        // Pressure-sensitive width: blend the two endpoints
        const avgPressure = (p1.p + p2.p) / 2;
        ctx.lineWidth = baseWidth * avgPressure * 2;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
        ctx.moveTo(p1.x, p1.y);
        ctx.lineTo(p2.x, p2.y);
        ctx.stroke();
    }

    // ── Expose ──────────────────────────────────────────────────────────────

    return {
        initCanvas: initCanvas,
        disposeCanvas: disposeCanvas,
        clearCanvas: clearCanvas,
        undoStroke: undoStroke,
        setColor: setColor,
        setLineWidth: setLineWidth,
        getStrokeData: getStrokeData,
        loadStrokeData: loadStrokeData,
        getStrokeCount: getStrokeCount,
        resizeCanvas: resizeCanvas
    };
})();
