window.drawTool = {
    getCanvasMetrics: function (element) {
        if (!element) {
            return { left: 0, top: 0, scrollLeft: 0, scrollTop: 0 };
        }

        const rect = element.getBoundingClientRect();
        return {
            left: rect.left,
            top: rect.top,
            scrollLeft: element.scrollLeft,
            scrollTop: element.scrollTop
        };
    }
};
