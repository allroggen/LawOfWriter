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

    // Palm rejection state
    let activePointerId = null;
    let penDetected = false;
    let penTimeout = null;

    // Tool mode: 'draw' or 'erase'
    let currentMode = 'draw';
    let eraserRadius = 16;
    let erasedThisGesture = false; // track if eraser removed anything

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
        activePointerId = null;
        penDetected = false;

        // Prevent default touch actions (scroll, zoom, text selection)
        canvas.style.touchAction = 'none';
        canvas.style.userSelect = 'none';
        canvas.style.webkitUserSelect = 'none';
        canvas.style.webkitTouchCallout = 'none';

        // Attach pointer events
        canvas.addEventListener('pointerdown', onPointerDown);
        canvas.addEventListener('pointermove', onPointerMove);
        canvas.addEventListener('pointerup', onPointerUp);
        canvas.addEventListener('pointercancel', onPointerUp);
        canvas.addEventListener('pointerleave', onPointerUp);

        // Prevent context menu (long-press) on canvas
        canvas.addEventListener('contextmenu', function (e) { e.preventDefault(); });

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
        if (penTimeout) clearTimeout(penTimeout);
        canvas = null;
        ctx = null;
        dotNetRef = null;
        strokes = [];
        activePointerId = null;
        penDetected = false;
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

    function setMode(mode) {
        currentMode = mode; // 'draw' or 'erase'
        if (canvas) {
            canvas.style.cursor = mode === 'erase' ? 'crosshair' : 'crosshair';
        }
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

    // ── Palm rejection ──────────────────────────────────────────────────────

    function shouldRejectPointer(e) {
        // If a pen is active/nearby, reject touch input (palm on screen)
        if (e.pointerType === 'pen') {
            penDetected = true;
            if (penTimeout) clearTimeout(penTimeout);
            // Keep pen priority for 500ms after last pen event
            penTimeout = setTimeout(function () { penDetected = false; }, 500);
            return false; // pen is always accepted
        }

        if (e.pointerType === 'touch' && penDetected) {
            return true; // reject touch while pen is active
        }

        // If another pointer is already drawing, reject additional pointers
        if (activePointerId !== null && e.pointerId !== activePointerId) {
            return true;
        }

        return false; // accept (mouse or touch when no pen)
    }

    // ── Eraser logic ────────────────────────────────────────────────────────

    function eraseAtPoint(x, y) {
        const r = eraserRadius;
        let removed = false;
        for (let i = strokes.length - 1; i >= 0; i--) {
            const pts = strokes[i].points;
            for (let j = 0; j < pts.length; j++) {
                const dx = pts[j].x - x;
                const dy = pts[j].y - y;
                if (dx * dx + dy * dy <= r * r) {
                    strokes.splice(i, 1);
                    removed = true;
                    break;
                }
            }
        }
        if (removed) {
            redraw();
            // Draw eraser cursor indicator
            drawEraserCursor(x, y);
        }
        return removed;
    }

    function drawEraserCursor(x, y) {
        if (!ctx) return;
        ctx.save();
        ctx.beginPath();
        ctx.arc(x, y, eraserRadius, 0, Math.PI * 2);
        ctx.strokeStyle = 'rgba(200, 0, 0, 0.4)';
        ctx.lineWidth = 1.5;
        ctx.stroke();
        ctx.restore();
    }

    // ── Drawing logic ───────────────────────────────────────────────────────

    function onPointerDown(e) {
        e.preventDefault();
        if (shouldRejectPointer(e)) return;

        activePointerId = e.pointerId;
        isDrawing = true;
        canvas.setPointerCapture(e.pointerId);

        if (currentMode === 'erase') {
            erasedThisGesture = false;
            const pt = getPoint(e);
            if (eraseAtPoint(pt.x, pt.y)) {
                erasedThisGesture = true;
            }
            return;
        }

        const point = getPoint(e);
        currentStroke = {
            points: [point],
            color: currentColor,
            width: currentWidth
        };
    }

    function onPointerMove(e) {
        e.preventDefault();
        if (e.pointerId !== activePointerId) return;

        if (currentMode === 'erase' && isDrawing) {
            const pt = getPoint(e);
            if (eraseAtPoint(pt.x, pt.y)) {
                erasedThisGesture = true;
            }
            return;
        }

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
        if (e.pointerId !== activePointerId) return;

        if (currentMode === 'erase' && isDrawing) {
            isDrawing = false;
            activePointerId = null;
            if (erasedThisGesture) {
                redraw();
                notifyChanged();
            }
            erasedThisGesture = false;
            return;
        }

        if (!isDrawing) return;
        isDrawing = false;
        activePointerId = null;

        if (currentStroke && currentStroke.points.length > 0) {
            strokes.push(currentStroke);
            notifyChanged();
        }
        currentStroke = null;
    }

    function notifyChanged() {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnCanvasChanged')
                .catch(function (err) { console.warn('[drawingCanvas] OnCanvasChanged failed:', err); });
        }
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
        setMode: setMode,
        getStrokeData: getStrokeData,
        loadStrokeData: loadStrokeData,
        getStrokeCount: getStrokeCount,
        resizeCanvas: resizeCanvas
    };
})();
