/// <reference path="three.min.js" />
/// <reference path="hammer.min.js" />

"use strict";

//The distance to pan depends on the zoom level, we therefore need to store the original camDistance and original panDistance
var orgCamDistance = 300;
var orgPanDistance = 1;

//We also store the currrent camDistance as a global
var camDistance = orgCamDistance;

//Grid creation functions

var boxX = 100;
var boxY = 200;
var boxZ = 100;

function makeGrid(startX, startY, startZ, xLength, yLength, zLength, interval, lineColor) {
    var material = new THREE.LineBasicMaterial({
        color: "white"
    });

    var geometry = new THREE.Geometry();

    if (zLength == 0) {
        //XY
        var countX = Math.round(xLength / interval);
        var countY = Math.round(yLength / interval);

        for (var i = 0; i < countX; i++) {
            var lineX = startX + interval * i;
            geometry.vertices.push(new THREE.Vector3(lineX, 0, startZ));
            geometry.vertices.push(new THREE.Vector3(lineX, yLength, startZ));
        }

        geometry.vertices.push(new THREE.Vector3(xLength, 0, startZ));
        geometry.vertices.push(new THREE.Vector3(xLength, yLength, startZ));

        for (var i = 0; i < countY; i++) {
            var lineY = startY + interval * i;
            geometry.vertices.push(new THREE.Vector3(0, lineY, startZ));
            geometry.vertices.push(new THREE.Vector3(xLength, lineY, startZ));
        }

        geometry.vertices.push(new THREE.Vector3(0, yLength, startZ));
        geometry.vertices.push(new THREE.Vector3(xLength, yLength, startZ));
    }
    else if (yLength == 0) {
        //XZ
        var countX = Math.round(xLength / interval);
        var countZ = Math.round(zLength / interval);

        for (var i = 0; i < countX; i++) {
            var lineX = startX + interval * i;
            geometry.vertices.push(new THREE.Vector3(lineX, startY, 0));
            geometry.vertices.push(new THREE.Vector3(lineX, startY, zLength));
        }

        geometry.vertices.push(new THREE.Vector3(xLength, startY, 0));
        geometry.vertices.push(new THREE.Vector3(xLength, startY, zLength));

        for (var i = 0; i < countZ; i++) {
            var lineZ = startZ + interval * i;
            geometry.vertices.push(new THREE.Vector3(0, startY, lineZ));
            geometry.vertices.push(new THREE.Vector3(xLength, startY, lineZ));
        }

        geometry.vertices.push(new THREE.Vector3(0, startY, zLength));
        geometry.vertices.push(new THREE.Vector3(xLength, startY, zLength));
    }
    else {
        //YZ
        var countY = Math.round(yLength / interval);
        var countZ = Math.round(zLength / interval);

        for (var i = 0; i < countY; i++) {
            var lineY = startY + interval * i;
            geometry.vertices.push(new THREE.Vector3(startX, lineY, 0));
            geometry.vertices.push(new THREE.Vector3(startX, lineY, zLength));
        }

        geometry.vertices.push(new THREE.Vector3(startX, yLength, 0));
        geometry.vertices.push(new THREE.Vector3(startX, yLength, zLength));

        for (var i = 0; i < countZ; i++) {
            var lineZ = startZ + interval * i;
            geometry.vertices.push(new THREE.Vector3(startX, 0, lineZ));
            geometry.vertices.push(new THREE.Vector3(startX, yLength, lineZ));
        }

        geometry.vertices.push(new THREE.Vector3(startX, 0, zLength));
        geometry.vertices.push(new THREE.Vector3(startX, yLength, zLength));
    }

    return new THREE.Line(geometry, material, THREE.LinePieces);
}

function makeGrids(gridXY, gridXZ, gridYZ) {
    var gridXY = makeGrid(0, 0, 0, boxX, boxY, 0, 10, "White");//0x000066);
    scene.add(gridXY);

    var gridXZ = makeGrid(0, boxY, 0, boxX, 0, boxZ, 10, "White");//0x006600);
    scene.add(gridXZ);

    var gridYZ = makeGrid(boxX, 0, 0, 0, boxY, boxZ, 10, "White");//0x660000);
    scene.add(gridYZ);
}

//Camera function

function updateCameraAssembly() {
    ringGeometry.vertices = new THREE.CircleGeometry(camDistance, 4).vertices;//new THREE.TorusGeometry(camDistance, 3, 8, 32).vertices;
    ringGeometry.verticesNeedUpdate = true;
    cameraPrism.position.set(camDistance, 0, 0);
    camera2.position.set(camDistance, 0, 0);
}

function initCameraAssembly() {
    rotObj = new THREE.Object3D();

    ringGeometry = new THREE.CircleGeometry(camDistance, 4);//new THREE.TorusGeometry(camDistance, 3, 8, 32);
    ringGeometry.dynamic = true;
    var material = new THREE.MeshLambertMaterial({ color: "red" });
    ring1 = new THREE.Mesh(ringGeometry, material);
    rotObj.add(ring1);

    ring2 = ring1.clone();
    ring2.rotation.x = Math.PI / 2;
    ring2.material = new THREE.MeshLambertMaterial({ color: "blue" });
    rotObj.add(ring2);

    var triPoints = [];
    triPoints.push(new THREE.Vector2(-10, -10));
    triPoints.push(new THREE.Vector2(10, -10));
    triPoints.push(new THREE.Vector2(10, 0));
    triPoints.push(new THREE.Vector2(0, 10));
    triPoints.push(new THREE.Vector2(-10, 0));
    var triShape = new THREE.Shape(triPoints);
    var extrusionSettings = {
        bevelEnabled: false, amount: 10
    };
    var triGeometry = new THREE.ExtrudeGeometry(triShape, extrusionSettings);
    material = new THREE.MeshNormalMaterial();
    cameraPrism = new THREE.Mesh(triGeometry, material);
    cameraPrism.rotation.y = -Math.PI / 2;
    cameraPrism.position.set(camDistance, 0, 0);
    rotObj.add(cameraPrism);

    camera2.position.set(camDistance, 0, 0);
    rotObj.add(camera2);
    camera2.lookAt(new THREE.Vector3(0, 0, 0));

    rotObj.position.set(boxX / 2, boxY / 2, 0);
    rotObj.rotateX(-Math.PI / 2);
    rotObj.rotateY(-Math.PI / 4);
    rotObj.rotateZ(-Math.PI / 4 * 3);

    ring1.visible = true;
    ring2.visible = true;
    cameraPrism.visible = false;

    scene.add(rotObj);
}

//View Control functions

var defaultRotAmount = 0.05;
var minCamDist = 1;
var defaultPanAmount = 5;
var defaultZoomAmount = 0.05;

function rotateLeft(distance) {
    if (!distance && distance != 0)
        distance = defaultRotAmount;

    rotObj.rotateY(distance);
}

function rotateRight(distance) {
    if (!distance && distance != 0)
        distance = defaultRotAmount;

    rotObj.rotateY(-distance);
}

function rotateUp(distance) {
    if (!distance && distance != 0)
        distance = defaultRotAmount;

    rotObj.rotateZ(-distance);
}

function rotateDown(distance) {
    if (!distance && distance != 0)
        distance = defaultRotAmount;

    rotObj.rotateZ(distance);
}

//Zoom percentages are an amount out of 1

function zoomIn(percent) {
    if (!percent && percent != 0)
        percent = defaultZoomAmount;

    zoomScale(1 - percent);
}

function zoomOut(percent) {
    if (!percent && percent != 0)
        percent = defaultZoomAmount;

    zoomScale(1 + percent);
}

function zoomScale(scale) {
    if (!scale || (camDistance < 1 && scale < 1))
        return;

    camDistance *= scale;
    updateCameraAssembly();
}

function pan(rightDist, downDist) {
    var camScale = camDistance / orgCamDistance;

    rotObj.translateZ(rightDist * camScale * orgPanDistance);
    rotObj.translateY(downDist * camScale * orgPanDistance);
}

function panLeft() {
    pan(-defaultPanAmount, 0);
}

function panRight() {
    pan(defaultPanAmount, 0);
}

function panUp() {
    pan(0, -defaultPanAmount);
}

function panDown() {
    pan(0, defaultPanAmount)
}

//Functions to handle user input

//Keyboard

function keyDown(event) {
    switch (event.key) {
        case 'w':
            rotateUp();
            break;
        case 's':
            rotateDown();
            break;
        case 'a':
            rotateLeft();
            break;
        case 'd':
            rotateRight();
            break;
        case '+':
            zoomIn();
            break;
        case 'Add':
            zoomIn();
            break;
        case '-':
            zoomOut();
            break;
        case 'Subrtact':
            zoomOut();
            break;
        case "Right":
            panRight();
            break;
        case "Left":
            panLeft();
            break;
        case "Up":
            panUp();
            break;
        case "Down":
            panDown();
            break;
    }
}

document.addEventListener('keydown', keyDown, false);

//Mouse and Touch

var STATE = { NONE: -1, ROTATE: 0, ZOOM: 1, PAN: 2, TOUCH_ROTATE: 3, TOUCH_ZOOM: 4, TOUCH_PAN: 5 };
var state = STATE.NONE;

var rotateStart = new THREE.Vector2();
var rotateEnd = new THREE.Vector2();
var rotateDelta = new THREE.Vector2();

var panStart = new THREE.Vector2();
var panEnd = new THREE.Vector2();
var panDelta = new THREE.Vector2();

var zoomStart = 0;
var zoomEnd = 0;
var zoomDelta = 0;

//Mouse

function mouseScroll(event) {
    var delta = event.wheelDelta;

    //TODO: maybe Firefox doesnt work
    if (delta > 0)
        zoomIn();
    else
        zoomOut();
}

function mouseDown(event) {
    //alert(event.button);
    switch (event.button) {

        case 0:	// left: rotate
            state = STATE.ROTATE;

            rotateStart.set(event.clientX, event.clientY);
            break;

        case 1:	// middle: zoom
            state = STATE.ZOOM;

            zoomStart = new THREE.Vector2(event.clientX, event.clientY);
            break;

        case 2: // right: pan
            state = STATE.PAN;

            panStart.set(event.clientX, event.clientY);
            break;

        default:
            state = STATE.NONE;

    }
}

function mouseMove(event) {
    switch (state) {
        case STATE.ROTATE:
            rotateEnd.set(event.clientX, event.clientY);
            rotateDelta.subVectors(rotateEnd, rotateStart);

            rotateRight(rotateDelta.x / 100);
            rotateDown(rotateDelta.y / 100);

            rotateStart.copy(rotateEnd);
            break;
        case STATE.ZOOM:
            zoomEnd = new THREE.Vector2(event.clientX, event.clientY);
            zoomDelta = new THREE.Vector2();
            zoomDelta.subVectors(zoomEnd, zoomStart);

            if (zoomDelta.y < 0)
                zoomIn();
            else
                zoomOut();

            zoomStart.copy(zoomEnd);
            break;

        case STATE.PAN:
            panEnd.set(event.clientX, event.clientY);
            panDelta.subVectors(panEnd, panStart);

            pan(panDelta.x / 2, panDelta.y / 2);

            panStart.copy(panEnd);
            break;
    }
}

function mouseUp() {
    state = STATE.NONE;
}

function initMouse(mouseelement) {
    mouseelement.addEventListener('mousewheel', mouseScroll, false);
    mouseelement.addEventListener('DOMMouseScroll', mouseScroll, false);
    mouseelement.addEventListener('mousedown', mouseDown, false);
    mouseelement.addEventListener('mouseup', mouseUp, false);
    mouseelement.addEventListener('mousemove', mouseMove, false);

    //Suppress right mouse clicks
    mouseelement.addEventListener('contextmenu', function (ev) {
        ev.preventDefault();
    });
}

//Touch

function touchStart(event) {
    switch (event.touches.length) {

        case 1:	// one-fingered touch: rotate
            state = STATE.TOUCH_ROTATE;

            rotateStart.set(event.touches[0].pageX, event.touches[0].pageY);
            break;

        case 2:	// two-fingered touch: zoom
            state = STATE.TOUCH_ZOOM;

            var dx = event.touches[0].pageX - event.touches[1].pageX;
            var dy = event.touches[0].pageY - event.touches[1].pageY;
            zoomStart = Math.sqrt(dx * dx + dy * dy);
            break;

        case 3: // three-fingered touch: pan
            state = STATE.TOUCH_PAN;

            panStart.set(event.touches[0].pageX, event.touches[0].pageY);
            break;

        default:
            state = STATE.NONE;

    }
}

function touchMove(event) {
    event.preventDefault();
    event.stopPropagation();

    //var element = scope.domElement === document ? scope.domElement.body : scope.domElement;

    switch (event.touches.length) {

        case 1: // one-fingered touch: rotate
            if (state !== STATE.TOUCH_ROTATE) { return; }

            rotateEnd.set(event.touches[0].pageX, event.touches[0].pageY);
            rotateDelta.subVectors(rotateEnd, rotateStart);

            // rotating across whole screen goes 360 degrees around
            //scope.rotateLeft(2 * Math.PI * rotateDelta.x / element.clientWidth * scope.rotateSpeed);
            // rotating up and down along whole screen attempts to go 360, but limited to 180
            //scope.rotateUp(2 * Math.PI * rotateDelta.y / element.clientHeight * scope.rotateSpeed);

            //console.log(rotateDelta);
            rotateRight(rotateDelta.x / 100);
            rotateDown(rotateDelta.y / 100);

            rotateStart.copy(rotateEnd);
            break;

        case 2: // two-fingered touch: zoom
            if (state !== STATE.TOUCH_ZOOM) { return; }

            var dx = event.touches[0].pageX - event.touches[1].pageX;
            var dy = event.touches[0].pageY - event.touches[1].pageY;
            zoomEnd = Math.sqrt(dx * dx + dy * dy);
            zoomDelta = zoomStart / zoomEnd;

            zoomScale(zoomDelta);

            zoomStart = zoomEnd;
            break;

        case 3: // three-fingered touch: pan
            if (state !== STATE.TOUCH_PAN) { return; }

            panEnd.set(event.touches[0].pageX, event.touches[0].pageY);
            panDelta.subVectors(panEnd, panStart);

            pan(panDelta.x, panDelta.y);

            panStart.copy(panEnd);
            break;

        default:
            state = STATE.NONE;

    }

}

function touchEnd() {
    state = STATE.NONE;
}

//Remember if the doubletap is zoomed in or not
var zoomed = false;

function doubleTap() {
    if (!zoomed) {
        zoomScale(0.25);
        zoomed = true;
    }
    else {
        zoomScale(4);
        zoomed = false;
    }
}

function initTouch(touchelement) {
    var hammertime = Hammer(touchelement);
    hammertime.options.preventMouse = true; //Mouse is handled above
    hammertime.on("touch", function (event) { touchStart(event.gesture); });
    hammertime.on("release", function (event) { touchEnd(); });
    hammertime.on("gesture", function (event) { touchMove(event.gesture); });
    //hammertime.on("doubletap", function (event) { doubleTap(); })
}

//The main initialize function

function initOrbitControls(touchelement, mouseelement) {
    initMouse(mouseelement);
    initTouch(touchelement);
}