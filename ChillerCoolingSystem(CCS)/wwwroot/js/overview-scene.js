import * as THREE from '/lib/three/three.module.js';
import { EffectComposer } from '/lib/three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from '/lib/three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from '/lib/three/addons/postprocessing/UnrealBloomPass.js';
import { OutputPass } from '/lib/three/addons/postprocessing/OutputPass.js';

const MACHINE_META = {
    ct3: { name: 'CT #3', kind: 'ct', x: -3.4, z: -3.6, hasTempIn: true },
    ct7: { name: 'CT #7', kind: 'ct', x: 3.4, z: -3.6, hasTempIn: true },
    cl3: { name: 'Chiller #3', kind: 'cl', x: -6.4, z: 2.2, hasTempIn: false },
    cl5: { name: 'Chiller #5', kind: 'cl', x: -3.2, z: 2.2, hasTempIn: false },
    cl8: { name: 'Chiller #8', kind: 'cl', x: 0, z: 2.2, hasTempIn: false },
    cl9: { name: 'Chiller #9', kind: 'cl', x: 3.2, z: 2.2, hasTempIn: false },
    cl10: { name: 'Chiller #10', kind: 'cl', x: 6.4, z: 2.2, hasTempIn: false }
};

function classify(snapshot, key) {
    var runStop = snapshot[key + 'RunStop'];
    var fault = snapshot[key + 'Fault'];
    if (!runStop || !runStop.good || !fault || !fault.good) return 'error';
    if (fault.value !== '0') return 'error';
    return runStop.value === '1' ? 'running' : 'stopped';
}

var STATUS_COLOR = { running: 0x22c55e, stopped: 0xef4444, error: 0x94a3b8 };

// Ảnh sản phẩm nền trắng đặc (không có kênh alpha) — tự "key" nền trắng thành
// trong suốt bằng canvas 2D để dùng làm billboard trong scene tối, không cần
// tải/chỉnh sửa ảnh ngoài.
function loadKeyedTexture(url) {
    return new Promise(function (resolve, reject) {
        var img = new Image();
        img.onload = function () {
            var canvas = document.createElement('canvas');
            canvas.width = img.width;
            canvas.height = img.height;
            var ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0);

            var frame = ctx.getImageData(0, 0, canvas.width, canvas.height);
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

            var texture = new THREE.CanvasTexture(canvas);
            texture.colorSpace = THREE.SRGBColorSpace;
            resolve({ texture: texture, aspect: img.width / img.height });
        };
        img.onerror = reject;
        img.src = url;
    });
}

// Sàn KHÔNG lặp (repeat 1x1) để có thể vẽ quầng sáng đúng vị trí 2 trụ đèn —
// kẻ ô rất mờ/thưa (chỉ gợi ý kết cấu sàn công nghiệp) thay vì lưới rõ như
// giấy kẻ ô, tránh cảm giác "nền game" đơn điệu.
function buildGroundTexture(planeWidth, planeDepth, poleWorldPositions) {
    var w = 1024, h = Math.round(1024 * planeDepth / planeWidth);
    var canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    var ctx = canvas.getContext('2d');

    ctx.fillStyle = '#0d1119';
    ctx.fillRect(0, 0, w, h);

    function worldToPx(x, z) {
        return [(x / planeWidth + 0.5) * w, (z / planeDepth + 0.5) * h];
    }

    poleWorldPositions.forEach(function (p) {
        var px = worldToPx(p[0], p[1]);
        var r = w * 0.11;
        var grad = ctx.createRadialGradient(px[0], px[1], 0, px[0], px[1], r);
        grad.addColorStop(0, 'rgba(255,186,110,.20)');
        grad.addColorStop(1, 'rgba(255,186,110,0)');
        ctx.fillStyle = grad;
        ctx.fillRect(px[0] - r, px[1] - r, r * 2, r * 2);
    });

    ctx.strokeStyle = 'rgba(255,255,255,.035)';
    ctx.lineWidth = 1;
    var step = w / 16;
    for (var i = 0; i <= w; i += step) {
        ctx.beginPath(); ctx.moveTo(i, 0); ctx.lineTo(i, h); ctx.stroke();
    }
    for (var j = 0; j <= h; j += step) {
        ctx.beginPath(); ctx.moveTo(0, j); ctx.lineTo(w, j); ctx.stroke();
    }

    var texture = new THREE.CanvasTexture(canvas);
    return texture;
}

function buildTree(x, z) {
    var group = new THREE.Group();
    var trunk = new THREE.Mesh(
        new THREE.CylinderGeometry(0.12, 0.16, 1.1, 6),
        new THREE.MeshStandardMaterial({ color: 0x2b1f16, roughness: 1 })
    );
    trunk.position.y = 0.55;
    group.add(trunk);

    var foliageMat = new THREE.MeshStandardMaterial({ color: 0x14371f, roughness: 1 });
    for (var i = 0; i < 3; i++) {
        var cone = new THREE.Mesh(new THREE.ConeGeometry(1.1 - i * 0.22, 1.1, 8), foliageMat);
        cone.position.y = 1.3 + i * 0.75;
        group.add(cone);
    }
    group.position.set(x, 0, z);
    return group;
}

function buildLightPole(x, z, scene) {
    var group = new THREE.Group();
    var pole = new THREE.Mesh(
        new THREE.CylinderGeometry(0.06, 0.08, 3.6, 8),
        new THREE.MeshStandardMaterial({ color: 0x2a2f3a, roughness: .6, metalness: .4 })
    );
    pole.position.y = 1.8;
    group.add(pole);

    var bulb = new THREE.Mesh(
        new THREE.SphereGeometry(0.16, 12, 12),
        new THREE.MeshStandardMaterial({ color: 0xffdb8a, emissive: 0xffb84d, emissiveIntensity: 2 })
    );
    bulb.position.y = 3.6;
    group.add(bulb);

    var light = new THREE.PointLight(0xffb84d, 18, 12, 2);
    light.position.y = 3.5;
    group.add(light);

    group.position.set(x, 0, z);
    scene.add(group);
}

export function initOverviewScene(canvas, wrap) {
    if (!canvas || !wrap || typeof THREE === 'undefined') {
        return;
    }

    var renderer;
    try {
        renderer = new THREE.WebGLRenderer({ canvas: canvas, antialias: true, alpha: false });
    } catch (e) {
        console.warn('Không khởi tạo được WebGL, bỏ qua scene 3D:', e);
        return;
    }

    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.05;
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;

    var scene = new THREE.Scene();
    scene.background = new THREE.Color(0x060912);
    scene.fog = new THREE.Fog(0x060912, 16, 38);

    // FOV hẹp hơn + camera lùi xa hơn (thay vì FOV rộng/gần) để giảm méo phối
    // cảnh ở rìa khung hình — nhìn "phẳng" đỡ hẳn, giống ảnh sản phẩm hơn.
    var camera = new THREE.PerspectiveCamera(32, 1, 0.1, 100);
    camera.position.set(0, 8.6, 17.5);
    camera.lookAt(0, 2.2, -1.5);

    // Bloom cho đèn LED/bóng đèn phát sáng rực — threshold cao để chỉ các
    // điểm sáng (emissive) mới nở sáng, không làm mờ nhòe cả cảnh.
    var composer = new EffectComposer(renderer);
    composer.addPass(new RenderPass(scene, camera));
    var bloomPass = new UnrealBloomPass(new THREE.Vector2(1, 1), 0.55, 0.4, 1.05);
    composer.addPass(bloomPass);
    composer.addPass(new OutputPass());

    scene.add(new THREE.AmbientLight(0x1c2c4a, 1.1));
    var hemi = new THREE.HemisphereLight(0x22304f, 0x05070c, 0.5);
    scene.add(hemi);
    var moon = new THREE.DirectionalLight(0x8fa5d9, 0.55);
    moon.position.set(-6, 10, 4);
    moon.castShadow = true;
    moon.shadow.mapSize.set(1024, 1024);
    moon.shadow.camera.left = -12;
    moon.shadow.camera.right = 12;
    moon.shadow.camera.top = 10;
    moon.shadow.camera.bottom = -10;
    moon.shadow.camera.near = 1;
    moon.shadow.camera.far = 30;
    moon.shadow.bias = -0.0015;
    moon.shadow.radius = 3;
    scene.add(moon);

    var GROUND_W = 40, GROUND_D = 26;
    var polePositions = [[-8.5, -1], [8.5, -1]];
    var ground = new THREE.Mesh(
        new THREE.PlaneGeometry(GROUND_W, GROUND_D),
        new THREE.MeshStandardMaterial({
            map: buildGroundTexture(GROUND_W, GROUND_D, polePositions),
            roughness: .6,
            metalness: .12
        })
    );
    ground.rotation.x = -Math.PI / 2;
    ground.receiveShadow = true;
    scene.add(ground);

    [-9, -5, 5, 9].forEach(function (x) { scene.add(buildTree(x, -7.5)); });
    polePositions.forEach(function (p) { buildLightPole(p[0], p[1], scene); });

    var raycastTargets = [];
    var machines = {};

    // Script module này load sau (defer) so với script SignalR trong view —
    // nếu snapshot đầu tiên đã tới trước khi chạy tới đây thì lấy lại ngay,
    // để máy vừa dựng xong có màu trạng thái đúng ngay từ đầu.
    var lastSnapshot = window.__ovLastSnapshot || null;

    function addPill(key) {
        var el = document.createElement('div');
        el.className = 'ov-scene-pill';
        el.innerHTML = '<div class="ov-scene-pill-label">' + MACHINE_META[key].name + '</div><div class="ov-scene-pill-status">--</div>';
        wrap.appendChild(el);
        return el;
    }

    function buildMachine(key, texInfo) {
        var meta = MACHINE_META[key];
        var group = new THREE.Group();
        var body, ledY, anchorY, billboardHeight, billboardWidth, frontZ;

        // Thân khối 3D được TÍNH THEO đúng tỉ lệ ảnh billboard (không hard-code
        // kích thước riêng) — trước đây thân nhỏ/hẹp hơn hẳn ảnh và cách ảnh
        // một khoảng hở lớn nên nhìn tách lớp như dán đè, giờ thân sát khít
        // ngay sau ảnh (chỉ cách 1 khoảng rất nhỏ để tránh z-fighting).
        if (meta.kind === 'ct') {
            billboardHeight = 3.6;
            billboardWidth = billboardHeight * texInfo.aspect;
            var radius = billboardWidth / 2;
            var bodyHeight = billboardHeight * 0.86;
            body = new THREE.Mesh(
                new THREE.CylinderGeometry(radius * 0.94, radius, bodyHeight, 24),
                new THREE.MeshStandardMaterial({ color: 0x8890a0, roughness: .55, metalness: .45 })
            );
            body.position.y = bodyHeight / 2;
            var rim = new THREE.Mesh(
                new THREE.CylinderGeometry(radius * 0.98, radius * 0.98, 0.22, 24),
                new THREE.MeshStandardMaterial({ color: 0x14171f, roughness: .8 })
            );
            rim.position.y = bodyHeight + 0.1;
            group.add(rim);
            ledY = bodyHeight + 0.4;
            frontZ = radius + 0.015;
        } else {
            billboardHeight = 2.1;
            billboardWidth = billboardHeight * texInfo.aspect;
            var boxDepth = 1.1;
            var boxHeight = billboardHeight * 0.9;
            body = new THREE.Mesh(
                new THREE.BoxGeometry(billboardWidth * 0.96, boxHeight, boxDepth),
                new THREE.MeshStandardMaterial({ color: 0x99a2b4, roughness: .55, metalness: .35 })
            );
            body.position.y = boxHeight / 2;
            ledY = boxHeight + 0.2;
            frontZ = boxDepth / 2 + 0.015;
        }

        body.userData.machineKey = key;
        body.castShadow = true;
        body.receiveShadow = true;
        group.add(body);
        raycastTargets.push(body);

        // Dùng MeshBasicMaterial (không phụ thuộc ánh sáng cảnh) cho billboard
        // vì ảnh sản phẩm thật đã có sẵn ánh sáng/đổ bóng riêng — nếu để vật
        // liệu ăn ánh sáng cảnh đêm (khá tối/ám xanh) ảnh sẽ bị tối mờ đi.
        var billboard = new THREE.Mesh(
            new THREE.PlaneGeometry(billboardWidth, billboardHeight),
            new THREE.MeshBasicMaterial({ map: texInfo.texture, transparent: true, toneMapped: false, alphaTest: 0.5 })
        );
        billboard.position.set(0, billboardHeight / 2, frontZ);
        billboard.userData.machineKey = key;
        // alphaTest cho vật liệu ở trên giúp bóng đổ bám đúng hình cắt ảnh
        // (không đổ bóng thành cả khối chữ nhật của nền đã "key" trong suốt).
        billboard.castShadow = true;
        group.add(billboard);
        raycastTargets.push(billboard);

        var led = new THREE.Mesh(
            new THREE.SphereGeometry(0.11, 12, 12),
            new THREE.MeshStandardMaterial({ color: 0x94a3b8, emissive: 0x94a3b8, emissiveIntensity: 2.5, toneMapped: false })
        );
        led.position.set(0, ledY, frontZ * 0.3);
        group.add(led);

        group.position.set(meta.x, 0, meta.z);
        scene.add(group);

        anchorY = ledY + 0.5;
        machines[key] = {
            group: group,
            led: led,
            billboard: billboard,
            pillEl: addPill(key),
            anchor: new THREE.Vector3(meta.x, anchorY, meta.z),
            status: null
        };
    }

    function updateMachineVisual(key, status) {
        var m = machines[key];
        if (!m || m.status === status) return;
        m.status = status;

        var color = STATUS_COLOR[status] || STATUS_COLOR.error;
        m.led.material.color.setHex(color);
        m.led.material.emissive.setHex(color);

        m.pillEl.classList.remove('status-running', 'status-stopped', 'status-error');
        m.pillEl.classList.add('status-' + status);
        var statusEl = m.pillEl.querySelector('.ov-scene-pill-status');
        if (statusEl) {
            statusEl.textContent = status === 'running' ? 'RUN' : (status === 'stopped' ? 'STOP' : 'LỖI');
        }
    }

    Promise.all([
        loadKeyedTexture('/image/CoolingTower.png'),
        loadKeyedTexture('/image/Chiller.png')
    ]).then(function (results) {
        var ctTex = results[0], clTex = results[1];
        Object.keys(MACHINE_META).forEach(function (key) {
            buildMachine(key, MACHINE_META[key].kind === 'ct' ? ctTex : clTex);
            if (lastSnapshot) {
                updateMachineVisual(key, classify(lastSnapshot, key));
            }
        });
    }).catch(function (err) {
        console.warn('Không tải được ảnh thiết bị cho scene 3D:', err);
    });

    var detailEl = document.createElement('div');
    detailEl.className = 'ov-scene-detail';
    detailEl.innerHTML = '<button type="button" class="ov-scene-detail-close">&times;</button>' +
        '<div class="ov-scene-detail-title"></div><div class="ov-scene-detail-body"></div>';
    wrap.appendChild(detailEl);
    detailEl.querySelector('.ov-scene-detail-close').addEventListener('click', function () {
        detailEl.classList.remove('open');
    });

    var hintEl = document.createElement('div');
    hintEl.className = 'ov-scene-hint';
    hintEl.textContent = 'Bấm vào máy để xem chi tiết';
    wrap.appendChild(hintEl);

    function renderDetail(key) {
        var meta = MACHINE_META[key];
        var s = lastSnapshot || {};
        var rows = [];

        if (meta.hasTempIn) {
            rows.push(['Nhiệt độ vào', fmtTemp(s[key + 'TempIn'])]);
        }
        rows.push(['Nhiệt độ ra', fmtTemp(s[key + 'TempOut'])]);
        if (meta.kind === 'ct') {
            rows.push(['Nhiệt độ môi trường', fmtTemp(s[key + 'TempAmbi'])]);
            rows.push(['Áp lực nước', fmtRaw(s[key + 'PreWater'])]);
        }
        rows.push(['Run - Stop', fmtRunStop(s[key + 'RunStop'])]);
        rows.push(['Fault', fmtFault(s[key + 'Fault'])]);

        detailEl.querySelector('.ov-scene-detail-title').textContent = meta.name;
        detailEl.querySelector('.ov-scene-detail-body').innerHTML = rows.map(function (r) {
            return '<div class="ov-scene-detail-row"><span>' + r[0] + '</span><span>' + r[1] + '</span></div>';
        }).join('');
        detailEl.classList.add('open');
    }

    function fmtTemp(tag) { return (tag && tag.good && tag.value != null) ? (tag.value + ' °C') : '--'; }
    function fmtRaw(tag) { return (tag && tag.good && tag.value != null) ? tag.value : '--'; }
    function fmtRunStop(tag) { return (tag && tag.good) ? (tag.value === '1' ? 'RUN' : 'STOP') : '--'; }
    function fmtFault(tag) { return (tag && tag.good) ? (tag.value === '0' ? 'Normal' : ('Lỗi (' + tag.value + ')')) : '--'; }

    window.__ovSceneApplySnapshot = function (snapshot) {
        lastSnapshot = snapshot;
        Object.keys(MACHINE_META).forEach(function (key) {
            updateMachineVisual(key, classify(snapshot, key));
        });

        var openKey = detailEl.getAttribute('data-open-key');
        if (openKey && detailEl.classList.contains('open')) {
            renderDetail(openKey);
        }
    };

    var raycaster = new THREE.Raycaster();
    var pointer = new THREE.Vector2();
    var hovered = null;

    function setPointerFromEvent(evt) {
        var rect = canvas.getBoundingClientRect();
        pointer.x = ((evt.clientX - rect.left) / rect.width) * 2 - 1;
        pointer.y = -((evt.clientY - rect.top) / rect.height) * 2 + 1;
    }

    canvas.addEventListener('pointermove', function (evt) {
        setPointerFromEvent(evt);
        raycaster.setFromCamera(pointer, camera);
        var hits = raycaster.intersectObjects(raycastTargets, false);
        var key = hits.length > 0 ? hits[0].object.userData.machineKey : null;

        if (key !== hovered) {
            hovered = key;
            canvas.style.cursor = key ? 'pointer' : 'default';
        }
    });

    canvas.addEventListener('click', function (evt) {
        setPointerFromEvent(evt);
        raycaster.setFromCamera(pointer, camera);
        var hits = raycaster.intersectObjects(raycastTargets, false);
        if (hits.length > 0) {
            var key = hits[0].object.userData.machineKey;
            detailEl.setAttribute('data-open-key', key);
            renderDetail(key);
        }
    });

    function resize() {
        var w = wrap.clientWidth, h = wrap.clientHeight;
        if (w === 0 || h === 0) return;
        renderer.setSize(w, h, false);
        composer.setSize(w, h);
        camera.aspect = w / h;
        camera.updateProjectionMatrix();
    }

    if (window.ResizeObserver) {
        new ResizeObserver(resize).observe(wrap);
    } else {
        window.addEventListener('resize', resize);
    }
    resize();

    var projected = new THREE.Vector3();

    function updatePillPositions() {
        var w = wrap.clientWidth, h = wrap.clientHeight;
        Object.keys(machines).forEach(function (key) {
            var m = machines[key];
            projected.copy(m.anchor).project(camera);
            var visible = projected.z < 1;
            m.pillEl.style.display = visible ? '' : 'none';
            if (visible) {
                m.pillEl.style.left = ((projected.x * 0.5 + 0.5) * w) + 'px';
                m.pillEl.style.top = ((-projected.y * 0.5 + 0.5) * h) + 'px';
            }
        });
    }

    function animate() {
        requestAnimationFrame(animate);
        updatePillPositions();
        composer.render();
    }
    animate();
}
