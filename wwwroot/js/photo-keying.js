(function () {
    function keyImage(img) {
        var canvas = document.createElement('canvas');
        canvas.width = img.naturalWidth;
        canvas.height = img.naturalHeight;
        var ctx = canvas.getContext('2d');
        ctx.drawImage(img, 0, 0);

        var frame;
        try {
            frame = ctx.getImageData(0, 0, canvas.width, canvas.height);
        } catch (e) {
            return;
        }

        var px = frame.data;
        for (var i = 0; i < px.length; i += 4) {
            var lum = (px[i] + px[i + 1] + px[i + 2]) / 3;
            if (lum > 246) {
                px[i + 3] = 0;
            } else if (lum > 222) {
                px[i + 3] = Math.round(255 * (246 - lum) / (246 - 222));
            }
        }
        ctx.putImageData(frame, 0, 0);
        img.src = canvas.toDataURL('image/png');
    }

    function applyTo(img) {
        if (img.complete && img.naturalWidth > 0) {
            keyImage(img);
        } else {
            img.addEventListener('load', function () { keyImage(img); }, { once: true });
        }
    }

    window.ccsKeyWhiteBackground = function (selector) {
        document.querySelectorAll(selector).forEach(applyTo);
    };
})();
