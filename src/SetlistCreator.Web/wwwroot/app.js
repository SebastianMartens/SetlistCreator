window.setlistPrint = function (itemCount) {
    // Temporarily apply the print layout before measuring so the hidden header
    // and the flex list are sized exactly like the final A4 print view.
    const LINE_HEIGHT = 1.1;
    const MAX_FONT    = 100;
    const SAFETY_PX   = 2;

    function fitListToPage(list, initialFontSize) {
        let fontSize = initialFontSize;

        while (fontSize > 10) {
            list.style.fontSize = fontSize + 'px';

            if (list.scrollHeight <= list.clientHeight) {
                return;
            }

            fontSize--;
        }
    }

    function beforePrint() {
        document.body.classList.add('setlist-print-sizing');

        const list = document.querySelector('.setlist-print-content');
        if (!list || itemCount === 0) return;

        list.dataset.prevFontSize = list.style.fontSize;
        const itemH = list.getBoundingClientRect().height / itemCount;
        const fontSize = Math.max(10, Math.min(Math.floor(itemH / LINE_HEIGHT) - SAFETY_PX, MAX_FONT));
        fitListToPage(list, fontSize);
    }

    function afterPrint() {
        document.body.classList.remove('setlist-print-sizing');

        const list = document.querySelector('.setlist-print-content');
        if (!list) return;
        list.style.fontSize = list.dataset.prevFontSize || '';
        delete list.dataset.prevFontSize;
    }

    window.onbeforeprint = beforePrint;
    window.onafterprint = afterPrint;
    window.print();
};
