// Saves text as a file in the browser (called from Blazor through IJSRuntime).
window.downloadFile = (fileName, content) => {
    // BOM so Excel opens UTF-8 correctly
    const blob = new Blob(["\uFEFF", content], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
};
