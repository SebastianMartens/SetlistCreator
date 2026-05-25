window.setlistPrint = function (itemCount) {
    // A4 portrait: 297mm tall, 15mm margins top+bottom → 267mm content height.
    // At 96 dpi: 267mm × (96/25.4) ≈ 1009 px.
    // The list gets the remaining height after the actual header is rendered.
    // Items share that remaining height equally (flex:1). Largest font that fits:
    // itemHeight / lineHeight.
    const MM_TO_PX    = 96 / 25.4;
    const AVAILABLE_H = Math.round((297 - 2 * 15) * MM_TO_PX); // ≈ 1009 px
    const LINE_HEIGHT = 1.1;     // matches print CSS
    const MAX_FONT    = 100;

    function beforePrint() {
        const list = document.querySelector('.setlist-print-content');
        const header = document.querySelector('.setlist-print-header');
        if (!list || itemCount === 0) return;
        list.dataset.prevFontSize = list.style.fontSize;
        const headerH  = header ? header.getBoundingClientRect().height : 0;
        const itemH    = Math.max(0, AVAILABLE_H - headerH) / itemCount;
        const fontSize = Math.max(10, Math.min(Math.floor(itemH / LINE_HEIGHT), MAX_FONT));
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
