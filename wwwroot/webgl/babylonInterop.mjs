// Pure ES module; avoid global wrapper/IIFE transformations

const registry = new Map();
let nextId = 1;

function addRef(obj) {
  const id = nextId++;
  registry.set(id, obj);
  return { __jsObjRefId: id };
}

function getRef(id) {
  const obj = registry.get(id);
  if (!obj) throw new Error(`Object ref ${id} not found`);
  return obj;
}

export function removeObjectRef(id) {
  registry.delete(id);
}

function getCanvas(canvasId) {
  const canvas = document.getElementById(canvasId);
  if (!canvas) throw new Error(`Canvas '${canvasId}' not found`);
  return canvas;
}

export function createEngine(canvasId, antialias = false) {
  if (!window.BABYLON) throw new Error("BABYLON is not loaded");
  const canvas = getCanvas(canvasId);
  const engine = new window.BABYLON.Engine(canvas, antialias);
  return addRef(engine);
}

export function createScene(engineRef) {
  const engine = getRef(engineRef.__jsObjRefId);
  const scene = new window.BABYLON.Scene(engine);
  return addRef(scene);
}

export function createVector3(x, y, z) {
  const v = new window.BABYLON.Vector3(x, y, z);
  return addRef(v);
}

export function createSphere(name, optionsRef, sceneRef) {
  const scene = getRef(sceneRef.__jsObjRefId);
  const opts = optionsRef ?? {};
  const mesh = window.BABYLON.MeshBuilder.CreateSphere(name, opts, scene);
  return addRef(mesh);
}

export function createArcRotateCamera(name, alpha, beta, radius, targetRef, sceneRef, canvasId) {
  const scene = getRef(sceneRef.__jsObjRefId);
  const target = getRef(targetRef.__jsObjRefId);
  const camera = new window.BABYLON.ArcRotateCamera(name, alpha, beta, radius, target, scene);
  camera.attachControl(getCanvas(canvasId), true);
  return addRef(camera);
}

export function createHemisphericLight(name, directionRef, sceneRef) {
  const scene = getRef(sceneRef.__jsObjRefId);
  const direction = getRef(directionRef.__jsObjRefId);
  const light = new window.BABYLON.HemisphericLight(name, direction, scene);
  return addRef(light);
}

export function createPointLight(name, directionRef, sceneRef) {
  const scene = getRef(sceneRef.__jsObjRefId);
  const direction = getRef(directionRef.__jsObjRefId);
  const light = new window.BABYLON.PointLight(name, direction, scene);
  return addRef(light);
}

export function runRenderLoop(engineRef, sceneRef) {
  const engine = getRef(engineRef.__jsObjRefId);
  const scene = getRef(sceneRef.__jsObjRefId);
  engine.runRenderLoop(() => {
    scene.render();
  });
}