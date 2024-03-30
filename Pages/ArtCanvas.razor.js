export function consoleLog(msg) {
    console.log(msg);
}

export async function getImageDimensions(content) {
    const url = URL.createObjectURL(new Blob([await content.arrayBuffer()]))
    const dimensions = await new Promise(resolve => {
        const img = new Image();
        img.onload = function () {
            const data = { Width: img.naturalWidth, Height: img.naturalHeight };
            resolve(data);
        };
        img.src = url;
    });
    return JSON.stringify(dimensions);
}

window.previewImage = (inputElement, canvasElement, cleanCanvasElement) => {
    const imgLink = URL.createObjectURL(inputElement.files[0]);
    const img = new Image();
    img.src = imgLink;
    var targetImg = img;
    targetImg.onload = function () {
        var canvas = canvasElement;
        var cleanCanvas = cleanCanvasElement;
        canvas.width = cleanCanvas.width = targetImg.naturalWidth;
        canvas.height = cleanCanvas.height = targetImg.naturalHeight;
        var canvasStyleWidth = getClosestWidthForContainer(targetImg);
        canvas.style.width = canvasStyleWidth + 'px';
        canvas.style.height = (canvasStyleWidth * (canvas.height / canvas.width)) + 'px';

        canvas.getContext("2d").drawImage(targetImg, 0, 0);
        cleanCanvas.getContext("2d").drawImage(targetImg, 0, 0);
    }
}

export function saveImage(canvasId) {
    const canvas = document.getElementById(canvasId);
    const image = canvas.toDataURL("image/png");

    let link = document.createElement('a');
    link.download = "image-edit.png";
    link.href = image;
    link.click();
}

export function resetImage(canvasId, cleanCanvasId) {
    var canvas = document.getElementById(canvasId);
    var cleanCanvas = document.getElementById(cleanCanvasId);
    var ctx = canvas.getContext("2d");
    ctx.drawImage(cleanCanvas, 0, 0);
}

export function applyFloydSteinbergDither(canvasId, optionsStr) {
    var canvas = document.getElementById(canvasId);
    var ctx = canvas.getContext("2d");
    var width = canvas.width;
    var height = canvas.height;
    const imageData = ctx.getImageData(0, 0, width, height);
    const data = imageData.data;
    const correctionValues = Array(data.length).fill(0);
    const thresholdValue = 255 * 3 / 2;
    for (let i = 0; i < data.length; i += 4) {
        var accumulatedValue = data[i] + data[i + 1] + data[i + 2] + (correctionValues[i] ?? 0);
        var correctionValue = 0;
        if (accumulatedValue >= thresholdValue) {
            data[i] = 255;
            data[i + 1] = 255;
            data[i + 2] = 255;
            correctionValue = accumulatedValue - 765;
        } else {
            data[i] = 0;
            data[i + 1] = 0;
            data[i + 2] = 0;
            correctionValue = accumulatedValue;
        }
        var coords = getCoordsForIndex(i, canvas);
        if (!coordIsOnRightEdge(coords, canvas)) {
            let rightIndex = getIndexForCoords([coords[0] + 1, coords[1]], canvas);
            let addedValue = Math.round(correctionValue * 7.0 / 16.0);
            let existingValue = (correctionValues[rightIndex] ?? 0.0);
            correctionValues[rightIndex] = addedValue + existingValue;
        }
        if (!coordIsOnLeftEdge(coords, canvas) && !coordIsOnBottomEdge(coords, canvas)) {
            let bottomLeftIndex = getIndexForCoords([coords[0] - 1, coords[1] + 1], canvas);
            correctionValues[bottomLeftIndex] = Math.round(correctionValue * 3.0 / 16.0) + (correctionValues[bottomLeftIndex] ?? 0.0);
        }
        if (!coordIsOnBottomEdge(coords, canvas)) {
            let bottomIndex = getIndexForCoords([coords[0], coords[1] + 1], canvas);
            correctionValues[bottomIndex] = Math.round(correctionValue * 5.0 / 16.0) + (correctionValues[bottomIndex] ?? 0.0);
        }
        if (!coordIsOnRightEdge(coords, canvas) && !coordIsOnBottomEdge(coords, canvas)) {
            let bottomRightIndex = getIndexForCoords([coords[0] + 1, coords[1] + 1], canvas);
            correctionValues[bottomRightIndex] = Math.round(correctionValue * 1.0 / 16.0) + (correctionValues[bottomRightIndex] ?? 0.0);
        }
    }
    ctx.putImageData(imageData, 0, 0);
}

export function applyOrderedDither(canvasId, optionsStr) {
    var canvas = document.getElementById(canvasId);
    var ctx = canvas.getContext("2d");
    var width = canvas.width;
    var height = canvas.height;
    const imageData = ctx.getImageData(0, 0, width, height);
    const data = imageData.data;
    let options = JSON.parse(optionsStr);
    let grayscale = options.grayscale.BinaryValue;
    let strength = options.strength.Value;
    let colorBits = options.colorBits.Value;
    let _2Matrix = [
        [0, 2],
        [3, 1]
    ]
    let _4Matrix = [
        [0, 8, 2, 10],
        [12, 4, 14, 6],
        [3, 11, 1, 9],
        [15, 7, 13, 5]
    ]
    let _8Matrix = [
        [0, 32, 8, 40, 2, 34, 10, 42],
        [48, 16, 56, 24, 50, 18, 58, 26],
        [12, 44, 4, 36, 14, 46, 6, 38],
        [60, 28, 52, 20, 62, 30, 54, 22],
        [3, 35, 11, 43, 1, 33, 9, 41],
        [51, 19, 59, 27, 49, 17, 57, 25],
        [15, 47, 7, 39, 13, 45, 5, 37],
        [63, 31, 55, 23, 61, 29, 53, 21]
    ]
    let _16Matrix = [
        [0, 191, 48, 239, 12, 203, 60, 251, 3, 194, 51, 242, 15, 206, 63, 254],
        [127, 64, 175, 112, 139, 76, 187, 124, 130, 67, 178, 115, 142, 79, 190, 127],
        [32, 223, 16, 207, 44, 235, 28, 219, 35, 226, 19, 210, 47, 238, 31, 222],
        [159, 96, 143, 80, 171, 108, 155, 92, 162, 99, 146, 83, 174, 111, 158, 95],
        [8, 199, 56, 247, 4, 195, 52, 243, 11, 202, 59, 250, 7, 198, 55, 246],
        [135, 72, 183, 120, 131, 68, 179, 116, 138, 75, 186, 123, 134, 71, 182, 119],
        [40, 231, 24, 215, 36, 227, 20, 211, 43, 234, 27, 218, 39, 230, 23, 214],
        [167, 104, 151, 88, 163, 100, 147, 84, 170, 107, 154, 91, 166, 103, 150, 87],
        [2, 193, 50, 241, 14, 205, 62, 253, 1, 192, 49, 240, 13, 204, 61, 252],
        [129, 66, 177, 114, 141, 78, 189, 126, 128, 65, 176, 113, 140, 77, 188, 125],
        [34, 225, 18, 209, 46, 237, 30, 221, 33, 224, 17, 208, 45, 236, 29, 220],
        [161, 98, 145, 82, 173, 110, 157, 94, 160, 97, 144, 81, 172, 109, 156, 93],
        [10, 201, 58, 249, 6, 197, 54, 245, 9, 200, 57, 248, 5, 196, 53, 244],
        [137, 74, 185, 122, 133, 70, 181, 118, 136, 73, 184, 121, 132, 69, 180, 117],
        [42, 233, 26, 217, 38, 229, 22, 213, 41, 232, 25, 216, 37, 228, 21, 212],
        [169, 106, 153, 90, 165, 102, 149, 86, 168, 105, 152, 89, 164, 101, 148, 85],
    ]
    let selectedMatrix = _2Matrix;
    let length = 2;
    let size = 4;
    if (strength == 2) {
        selectedMatrix = _4Matrix;
        length = 4;
        size = 16;
    } else if (strength == 3) {
        selectedMatrix = _8Matrix;
        length = 8;
        size = 64;
    } else if (strength == 4) {
        selectedMatrix = _16Matrix;
        length = 16;
        size = 256;
    }
    let normalizedThresholdMatrix = selectedMatrix.map(row => row.map(value => (value / size) - 0.5));
    let colorStep = 256 / colorBits | 0;
    if (strength == 0) {
        for (let i = 0; i < data.length; i += 4) {
            if (grayscale) {
                let normalizedValue = ((data[i] + data[i + 1] + data[i + 2]) / 3.0);
                data[i + 0] = data[i + 1] = data[i + 2] = getClosetValueNoDither(normalizedValue, colorStep);
            } else {
                data[i] = getClosetValueNoDither(data[i], colorStep);
                data[i + 1] = getClosetValueNoDither(data[i + 1], colorStep);
                data[i + 2] = getClosetValueNoDither(data[i + 2], colorStep);
            }
        }
    } else {
        for (let i = 0; i < data.length; i += 4) {
            let coords = getCoordsForIndex(i, canvas);
            let xPosition = coords[0] % length;
            let yPosition = coords[1] % length;
            let thresholdValue = normalizedThresholdMatrix[yPosition][xPosition];
            if (grayscale) {
                let normalizedValue = ((data[i] + data[i + 1] + data[i + 2]) / 3.0);
                data[i + 0] = data[i + 1] = data[i + 2] = getClosestValue(normalizedValue, thresholdValue, colorStep, colorBits);
            } else {
                data[i] = getClosestValue(data[i], thresholdValue, colorStep, colorBits);
                data[i + 1] = getClosestValue(data[i + 1], thresholdValue, colorStep, colorBits);
                data[i + 2] = getClosestValue(data[i + 2], thresholdValue, colorStep, colorBits);
            }
        }
    }
    ctx.putImageData(imageData, 0, 0);
}

function getClosetValueNoDither(val, colorStep) {
    var normalizedRGB = (val / colorStep);
    var roundedTotal = Math.round(normalizedRGB);
    var unnormalizedRGB = roundedTotal * colorStep;
    var clampedRGB = Math.max(0, Math.min(255, unnormalizedRGB));
    return clampedRGB;
}

function getClosestValue(val, thresholdValue, colorStep, steps) {
    var adjustedThresholdValue = thresholdValue / steps;
    var normalizedRGB = (val / colorStep);
    var addedThreshold = normalizedRGB + adjustedThresholdValue;
    var roundedTotal = Math.round(addedThreshold);
    var unnormalizedRGB = roundedTotal * colorStep;
    var clampedRGB = Math.max(0, Math.min(255, unnormalizedRGB));
    return clampedRGB;
}

export function applyInvert(canvasId, optionsStr) {
    var canvas = document.getElementById(canvasId);
    var ctx = canvas.getContext("2d");
    var width = canvas.width;
    var height = canvas.height;
    const imageData = ctx.getImageData(0, 0, width, height);
    const data = imageData.data;
    for (let i = 0; i < data.length; i += 4) {
        data[i] = 255 - data[i];
        data[i + 1] = 255 - data[i + 1];
        data[i + 2] = 255 - data[i + 2];
    }
    ctx.putImageData(imageData, 0, 0);
}

export function applyGammaCorrection(canvasId, optionsStr) {
    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext("2d");
    const width = canvas.width;
    const height = canvas.height;
    const imageData = ctx.getImageData(0, 0, width, height);
    const data = imageData.data;
    gammaCorrectData(data);
    ctx.putImageData(imageData, 0, 0);
}

export function applyPixelate(canvasId, optionsStr) {
    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext("2d");
    const width = canvas.width;
    const height = canvas.height;
    const imageData = ctx.getImageData(0, 0, width, height);
    const data = imageData.data;
    const options = JSON.parse(optionsStr);
    const strength = options.strength.Value;
    const pixelationStrength = 2 ** strength;
    for (let i = 0; i < data.length; i += 4) {
        const coords = getCoordsForIndex(i, canvas);
        const parentX = coords[0] - (coords[0] % pixelationStrength);
        const parentY = coords[1] - (coords[1] % pixelationStrength);
        const parentIndex = getIndexForCoords([parentX, parentY], canvas);
        copyColorsToIndex(parentIndex, i, data);
    }
    ctx.putImageData(imageData, 0, 0);
}

function gammaCorrectData(data) {
    let correctFunction = function (value) {
        return Math.pow(value, .7);
    }
    for (let i = 0; i < data.length; i += 4) {
        data[i] = correctFunction(data[i]);
        data[i + 1] = correctFunction(data[i + 1]);
        data[i + 2] = correctFunction(data[i + 2]);
    }
    return data;
}

function getClosestWidthForContainer(img) {
    var containerWidth = 1150;

    var currentTestWidth = img.naturalWidth;
    while (true) {
        if (currentTestWidth < containerWidth) {
            return currentTestWidth;
        }
        currentTestWidth /= 2;
    }
}

function getCoordsForIndex(i, canvas) {
    var coords = [];
    var width = canvas.width;
    var height = canvas.height;
    var x = (i % (width * 4)) / 4;
    var tempI = i - (x * 4);
    var y = (tempI / (width * 4));
    coords.push(x, y);
    return coords;
}

function getIndexForCoords(coords, canvas) {
    var width = canvas.width;
    var height = canvas.height;
    var index = ((coords[0]) + (coords[1] * width)) * 4;
    return index;
}

function getColorIndicesForIndex(index) {
    return [index, index + 1, index + 2, index + 3];
}

function coordIsOnLeftEdge(coords, canvas) {
    return coords[0] == 0;
}

function coordIsOnRightEdge(coords, canvas) {
    return (coords[0] + 1) == canvas.width;
}

function coordIsOnBottomEdge(coords, canvas) {
    return (coords[1] + 1) == canvas.height;
}

function copyColorsToIndex(fromIndex, toIndex, colorData) {
    const [parentRedIndex, parentGreenIndex, parentBlueIndex, parentAlphaIndex] = getColorIndicesForIndex(fromIndex);
    const [childRedIndex, childGreenIndex, childBlueIndex, childAlphaIndex] = getColorIndicesForIndex(toIndex);
    colorData[childRedIndex] = colorData[parentRedIndex];
    colorData[childGreenIndex] = colorData[parentGreenIndex];
    colorData[childBlueIndex] = colorData[parentBlueIndex];
    colorData[childAlphaIndex] = colorData[parentAlphaIndex];
}