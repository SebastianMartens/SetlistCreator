window.setlistPrint = function (itemCount) {
    // Estimate available page content height:
    // Letter (11in) at 96dpi = 1056px; A4 (297mm) = 1123px.
    // Subtract typical browser default margins (~1in top+bottom = ~192px)
    // and the print header (venue + date ~80px).
    const PAGE_CONTENT_HEIGHT_PX = 1056 - 192 - 80;

    function beforePrint() {
        const list = document.querySelector('.setlist-print-content');
        if (!list || itemCount === 0) return;
        const fontSize = Math.max(10, Math.min(Math.floor(PAGE_CONTENT_HEIGHT_PX / itemCount * 0.62), 80));
        list.dataset.prevFontSize = list.style.fontSize;
        list.style.fontSize = fontSize + 'px';
    }

    function afterPrint() {
        const list = document.querySelector('.setlist-print-content');
        if (!list) return;
        list.style.fontSize = list.dataset.prevFontSize || '';
        delete list.dataset.prevFontSize;
    }

    window.onbeforeprint = beforePrint;
    window.onafterprint = afterPrint;
    window.print();
};
