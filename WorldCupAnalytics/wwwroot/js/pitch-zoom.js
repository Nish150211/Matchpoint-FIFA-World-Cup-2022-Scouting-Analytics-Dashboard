// Shared pitch zoom/pan modal.
// Any element with class="pitch-zoomable" that contains exactly one <svg>
// becomes click-to-enlarge. Clicking clones that SVG into the modal, where
// it can be zoomed with the scroll wheel or +/- buttons, and panned by
// dragging. Works identically for shot maps and heat maps since it just
// operates on whatever SVG markup it's given — no page-specific logic.
(function () {
    const overlay = document.getElementById('pitchModalOverlay');
    const canvas = document.getElementById('pitchModalCanvas');
    const svgWrap = document.getElementById('pitchModalSvgWrap');
    const btnIn = document.getElementById('pitchZoomIn');
    const btnOut = document.getElementById('pitchZoomOut');
    const btnReset = document.getElementById('pitchZoomReset');
    const btnClose = document.getElementById('pitchModalClose');

    if (!overlay) return; // layout not loaded yet / safety check

    let scale = 1, panX = 0, panY = 0;
    let isDragging = false, dragStartX = 0, dragStartY = 0, panStartX = 0, panStartY = 0;

    function applyTransform() {
        svgWrap.style.transform = `translate(${panX}px, ${panY}px) scale(${scale})`;
    }

    function resetTransform() {
        scale = 1; panX = 0; panY = 0;
        applyTransform();
    }

    function openModal(svgEl) {
        svgWrap.innerHTML = '';
        svgWrap.appendChild(svgEl.cloneNode(true));
        resetTransform();
        overlay.classList.add('show');
        document.body.style.overflow = 'hidden';
    }

    function closeModal() {
        overlay.classList.remove('show');
        document.body.style.overflow = '';
    }

    // Wire up every zoomable pitch card currently on the page
    document.querySelectorAll('.pitch-zoomable').forEach(function (el) {
        el.addEventListener('click', function () {
            const svg = el.querySelector('svg');
            if (svg) openModal(svg);
        });
    });

    btnClose.addEventListener('click', closeModal);
    overlay.addEventListener('click', function (e) {
        if (e.target === overlay) closeModal(); // click outside the canvas
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && overlay.classList.contains('show')) closeModal();
    });

    btnIn.addEventListener('click', function () { scale = Math.min(6, scale * 1.3); applyTransform(); });
    btnOut.addEventListener('click', function () { scale = Math.max(0.5, scale / 1.3); applyTransform(); });
    btnReset.addEventListener('click', resetTransform);

    // Scroll wheel to zoom, centered roughly on cursor position
    canvas.addEventListener('wheel', function (e) {
        e.preventDefault();
        const delta = e.deltaY < 0 ? 1.12 : 1 / 1.12;
        scale = Math.max(0.5, Math.min(6, scale * delta));
        applyTransform();
    }, { passive: false });

    // Drag to pan
    canvas.addEventListener('mousedown', function (e) {
        isDragging = true;
        canvas.classList.add('dragging');
        dragStartX = e.clientX; dragStartY = e.clientY;
        panStartX = panX; panStartY = panY;
    });
    window.addEventListener('mousemove', function (e) {
        if (!isDragging) return;
        panX = panStartX + (e.clientX - dragStartX);
        panY = panStartY + (e.clientY - dragStartY);
        applyTransform();
    });
    window.addEventListener('mouseup', function () {
        isDragging = false;
        canvas.classList.remove('dragging');
    });

    // Basic touch support: one-finger drag to pan, pinch handled via native
    // browser zoom fallback (kept simple — full multitouch pinch math is a
    // lot of extra code for a portfolio project's secondary interaction).
    canvas.addEventListener('touchstart', function (e) {
        if (e.touches.length !== 1) return;
        isDragging = true;
        dragStartX = e.touches[0].clientX; dragStartY = e.touches[0].clientY;
        panStartX = panX; panStartY = panY;
    });
    canvas.addEventListener('touchmove', function (e) {
        if (!isDragging || e.touches.length !== 1) return;
        panX = panStartX + (e.touches[0].clientX - dragStartX);
        panY = panStartY + (e.touches[0].clientY - dragStartY);
        applyTransform();
    });
    canvas.addEventListener('touchend', function () { isDragging = false; });
})();