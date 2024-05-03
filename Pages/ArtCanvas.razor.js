export function consoleLog(msg) {
    console.log(msg);
}

export async function setImage(content, canvasElement) {
    await new Promise(async (resolve) => {
        const url = URL.createObjectURL(new Blob([await content.arrayBuffer()]))
        const img = new Image();
        img.onload = function () {
            let canvas = canvasElement;
            canvas.width = img.naturalWidth;
            canvas.height = img.naturalHeight;
            let canvasStyleWidth = getClosestWidthForContainer(canvas.width);
            canvas.style.width = canvasStyleWidth + 'px';
            canvas.style.height = (canvasStyleWidth * (canvas.height / canvas.width)) + 'px';
            canvas.getContext("2d").drawImage(img, 0, 0);
            resolve(true);
        };
        img.src = url;
    });
}

export async function getDimensions(canvasElement) {
    let canvas = canvasElement;
    return { Width: canvas.width, Height: canvas.height };
}

export function getPixelDataFromCanvas(canvasId) {
    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext("2d");
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height)
    const dataArray = new Uint8Array(imageData.data);
    return dataArray;
}

export function setPixelDataToCanvas(canvasId, bytes) {
    console.log("Setting pixel data to canvas");
    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext("2d");
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const data = imageData.data;
    for (let i = 0; i < data.length; i += 4) {
        data[i + 0] = bytes[i + 0];
        data[i + 1] = bytes[i + 1];
        data[i + 2] = bytes[i + 2];
        data[i + 3] = bytes[i + 3];
    }
    ctx.putImageData(imageData, 0, 0);
}

export function saveImage(canvasId) {
    const canvas = document.getElementById(canvasId);
    const image = canvas.toDataURL("image/png");

    let link = document.createElement('a');
    link.download = "image-edit.png";
    link.href = image;
    link.click();
}

function getClosestWidthForContainer(width) {
    var containerWidth = 1150;

    var currentTestWidth = width;
    while (true) {
        if (currentTestWidth < containerWidth) {
            return currentTestWidth;
        }
        currentTestWidth /= 2;
    }
}