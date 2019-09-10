"use strict";

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

            zoomStart.set(event.clientX, event.clientY);
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
            zoomEnd.set(event.clientX, event.clientY);
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