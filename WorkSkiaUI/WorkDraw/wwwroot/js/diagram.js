window.workdraw = {
    getRect(el) {
        const r = el.getBoundingClientRect();
        return { left: r.left, top: r.top, width: r.width, height: r.height };
    },

    downloadText(filename, text, mime) {
        const blob = new Blob([text], { type: mime || "application/octet-stream" });
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = filename;
        a.click();
        URL.revokeObjectURL(a.href);
    },

    // 図のコンテンツ範囲(ワールド座標)を指定して PNG に書き出す
    exportPng(svgEl, filename, x, y, w, h, scale) {
        const clone = svgEl.cloneNode(true);
        clone.querySelectorAll("[data-noexport]").forEach(n => n.remove());
        const scene = clone.querySelector("#scene");
        if (scene) scene.removeAttribute("transform");
        clone.setAttribute("viewBox", `${x} ${y} ${w} ${h}`);
        clone.setAttribute("width", Math.round(w * scale));
        clone.setAttribute("height", Math.round(h * scale));
        clone.style.background = "#ffffff";

        const xml = new XMLSerializer().serializeToString(clone);
        const blob = new Blob([xml], { type: "image/svg+xml;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const img = new Image();
        img.onload = () => {
            const canvas = document.createElement("canvas");
            canvas.width = Math.round(w * scale);
            canvas.height = Math.round(h * scale);
            const ctx = canvas.getContext("2d");
            ctx.fillStyle = "#ffffff";
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.drawImage(img, 0, 0);
            URL.revokeObjectURL(url);
            canvas.toBlob(b => {
                const a = document.createElement("a");
                a.href = URL.createObjectURL(b);
                a.download = filename;
                a.click();
                URL.revokeObjectURL(a.href);
            }, "image/png");
        };
        img.src = url;
    },

    // Delete / Ctrl+Z などをドキュメント全体で拾って .NET に渡す
    initKeyboard(dotnetRef) {
        this._keyHandler = e => {
            const tag = (e.target.tagName || "").toLowerCase();
            if (tag === "input" || tag === "textarea" || e.target.isContentEditable) return;
            const keys = ["Delete", "Backspace", "Escape"];
            if (keys.includes(e.key) || (e.ctrlKey && ["z", "y", "Z", "Y"].includes(e.key))) {
                e.preventDefault();
                dotnetRef.invokeMethodAsync("OnGlobalKeyDown", e.key, e.ctrlKey, e.shiftKey);
            }
        };
        document.addEventListener("keydown", this._keyHandler);
    },

    disposeKeyboard() {
        if (this._keyHandler) document.removeEventListener("keydown", this._keyHandler);
        this._keyHandler = null;
    },

    focusElement(el) {
        if (el) { el.focus(); el.select?.(); }
    }
};
