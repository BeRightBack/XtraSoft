// ========================================
// Driving Game Module - Clean Implementation
// ========================================

// Object reference management for Blazor interop
const objRefs = new Map();
let objRefId = 0;

function storeRef(obj) {
    const id = objRefId++;
    objRefs.set(id, obj);
    return { __jsObjRefId: id };
}

function getRef(ref) {
    if (ref && typeof ref === 'object' && '__jsObjRefId' in ref) {
        return objRefs.get(ref.__jsObjRefId);
    }
    return ref;
}

export function removeObjectRef(id) {
    objRefs.delete(id);
}

// ========================================
// Keyboard Input
// ========================================
const keys = {};
document.addEventListener('keydown', (e) => {
    keys[e.code] = true;
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'KeyW', 'KeyA', 'KeyS', 'KeyD'].includes(e.code)) {
        e.preventDefault();
    }
});
document.addEventListener('keyup', (e) => { keys[e.code] = false; });

function getInput() {
    return {
        forward: (keys['KeyW'] || keys['ArrowUp'] ? 1 : 0) - (keys['KeyS'] || keys['ArrowDown'] ? 1 : 0),
        turn: (keys['KeyD'] || keys['ArrowRight'] ? 1 : 0) - (keys['KeyA'] || keys['ArrowLeft'] ? 1 : 0)
    };
}

// ========================================
// Simplex Noise for Terrain
// ========================================
class SimplexNoise {
    constructor(seed = 12345) {
        this.perm = new Uint8Array(512);
        const p = new Uint8Array(256);
        for (let i = 0; i < 256; i++) p[i] = i;
        
        let s = seed;
        for (let i = 255; i > 0; i--) {
            s = (s * 16807) % 2147483647;
            const j = s % (i + 1);
            [p[i], p[j]] = [p[j], p[i]];
        }
        
        for (let i = 0; i < 512; i++) this.perm[i] = p[i & 255];
    }

    noise3D(x, y, z) {
        const F3 = 1/3, G3 = 1/6;
        const s = (x + y + z) * F3;
        const i = Math.floor(x + s), j = Math.floor(y + s), k = Math.floor(z + s);
        const t = (i + j + k) * G3;
        const X0 = i - t, Y0 = j - t, Z0 = k - t;
        const x0 = x - X0, y0 = y - Y0, z0 = z - Z0;
        
        let i1, j1, k1, i2, j2, k2;
        if (x0 >= y0) {
            if (y0 >= z0) { i1=1; j1=0; k1=0; i2=1; j2=1; k2=0; }
            else if (x0 >= z0) { i1=1; j1=0; k1=0; i2=1; j2=0; k2=1; }
            else { i1=0; j1=0; k1=1; i2=1; j2=0; k2=1; }
        } else {
            if (y0 < z0) { i1=0; j1=0; k1=1; i2=0; j2=1; k2=1; }
            else if (x0 < z0) { i1=0; j1=1; k1=0; i2=0; j2=1; k2=1; }
            else { i1=0; j1=1; k1=0; i2=1; j2=1; k2=0; }
        }
        
        const x1 = x0 - i1 + G3, y1 = y0 - j1 + G3, z1 = z0 - k1 + G3;
        const x2 = x0 - i2 + 2*G3, y2 = y0 - j2 + 2*G3, z2 = z0 - k2 + 2*G3;
        const x3 = x0 - 1 + 3*G3, y3 = y0 - 1 + 3*G3, z3 = z0 - 1 + 3*G3;
        
        const ii = i & 255, jj = j & 255, kk = k & 255;
        const grad = (hash, gx, gy, gz) => {
            const h = hash & 15;
            const u = h < 8 ? gx : gy;
            const v = h < 4 ? gy : (h === 12 || h === 14 ? gx : gz);
            return ((h & 1) ? -u : u) + ((h & 2) ? -v : v);
        };
        
        let n = 0;
        let t0 = 0.6 - x0*x0 - y0*y0 - z0*z0;
        if (t0 > 0) { t0 *= t0; n += t0 * t0 * grad(this.perm[ii + this.perm[jj + this.perm[kk]]], x0, y0, z0); }
        let t1 = 0.6 - x1*x1 - y1*y1 - z1*z1;
        if (t1 > 0) { t1 *= t1; n += t1 * t1 * grad(this.perm[ii + i1 + this.perm[jj + j1 + this.perm[kk + k1]]], x1, y1, z1); }
        let t2 = 0.6 - x2*x2 - y2*y2 - z2*z2;
        if (t2 > 0) { t2 *= t2; n += t2 * t2 * grad(this.perm[ii + i2 + this.perm[jj + j2 + this.perm[kk + k2]]], x2, y2, z2); }
        let t3 = 0.6 - x3*x3 - y3*y3 - z3*z3;
        if (t3 > 0) { t3 *= t3; n += t3 * t3 * grad(this.perm[ii + 1 + this.perm[jj + 1 + this.perm[kk + 1]]], x3, y3, z3); }
        
        return 32 * n;
    }
}

// ========================================
// Planet Chunk System
// ========================================
class PlanetChunkSystem {
    constructor(scene, radius, subdivisions) {
        this.scene = scene;
        this.radius = radius;
        this.subdivisions = subdivisions;
        this.chunks = new Map();
        this.activeChunks = new Set();
        this.chunkDefinitions = [];
        this.noise = new SimplexNoise(12345);
        this.treeNoise = new SimplexNoise(67890);
        
        this.worldContainer = new BABYLON.TransformNode("world", scene);
        this.generateChunkDefinitions();
        this.createMaterials();
    }
    
    generateChunkDefinitions() {
        const t = (1 + Math.sqrt(5)) / 2;
        const vertices = [
            [-1, t, 0], [1, t, 0], [-1, -t, 0], [1, -t, 0],
            [0, -1, t], [0, 1, t], [0, -1, -t], [0, 1, -t],
            [t, 0, -1], [t, 0, 1], [-t, 0, -1], [-t, 0, 1]
        ].map(v => new BABYLON.Vector3(...v).normalize());
        
        const faces = [
            [0,11,5], [0,5,1], [0,1,7], [0,7,10], [0,10,11],
            [1,5,9], [5,11,4], [11,10,2], [10,7,6], [7,1,8],
            [3,9,4], [3,4,2], [3,2,6], [3,6,8], [3,8,9],
            [4,9,5], [2,4,11], [6,2,10], [8,6,7], [9,8,1]
        ];
        
        faces.forEach(f => {
            this.subdivideForChunks(vertices[f[0]], vertices[f[1]], vertices[f[2]], 2);
        });
    }
    
    subdivideForChunks(v0, v1, v2, depth) {
        if (depth === 0) {
            this.chunkDefinitions.push({
                id: this.chunkDefinitions.length,
                center: v0.add(v1).add(v2).scale(1/3).normalize(),
                vertices: [v0.clone(), v1.clone(), v2.clone()]
            });
            return;
        }
        const mid = (a, b) => a.add(b).scale(0.5).normalize();
        const m01 = mid(v0, v1), m12 = mid(v1, v2), m20 = mid(v2, v0);
        this.subdivideForChunks(v0, m01, m20, depth - 1);
        this.subdivideForChunks(v1, m12, m01, depth - 1);
        this.subdivideForChunks(v2, m20, m12, depth - 1);
        this.subdivideForChunks(m01, m12, m20, depth - 1);
    }
    
    createMaterials() {
        this.terrainMat = new BABYLON.StandardMaterial("terrain", this.scene);
        this.terrainMat.backFaceCulling = false;
        this.terrainMat.specularColor = new BABYLON.Color3(0.1, 0.1, 0.1);
        this.terrainMat.diffuseColor = new BABYLON.Color3(1, 1, 1);
        this.terrainMat.emissiveColor = new BABYLON.Color3(0.2, 0.2, 0.2);
        
        // Tree materials
        this.trunkMat = new BABYLON.StandardMaterial("trunk", this.scene);
        this.trunkMat.diffuseColor = new BABYLON.Color3(0.4, 0.25, 0.1);
        this.trunkMat.emissiveColor = new BABYLON.Color3(0.15, 0.1, 0.05);
        
        this.leavesMat = new BABYLON.StandardMaterial("leaves", this.scene);
        this.leavesMat.diffuseColor = new BABYLON.Color3(0.1, 0.5, 0.15);
        this.leavesMat.emissiveColor = new BABYLON.Color3(0.05, 0.2, 0.05);
    }
    
    getTerrainHeight(dir) {
        const n = dir.normalize();
        let h = 0, amp = 1, freq = 2, maxAmp = 0;
        for (let i = 0; i < 5; i++) {
            h += this.noise.noise3D(n.x * freq, n.y * freq, n.z * freq) * amp;
            maxAmp += amp;
            amp *= 0.5;
            freq *= 2;
        }
        return this.radius + (h / maxAmp) * this.radius * 0.02;
    }
    
    getTerrainColor(normalizedHeight) {
        if (normalizedHeight < 0.35) return new BABYLON.Color3(0.1, 0.3, 0.7);
        if (normalizedHeight < 0.65) return new BABYLON.Color3(0.2, 0.6, 0.2);
        if (normalizedHeight < 0.8) return new BABYLON.Color3(0.5, 0.4, 0.3);
        return new BABYLON.Color3(0.95, 0.95, 1.0);
    }
    
    buildChunk(def) {
        if (this.chunks.has(def.id)) return this.chunks.get(def.id);
        
        const positions = [], indices = [], colors = [];
        this.buildTriangle(def.vertices[0], def.vertices[1], def.vertices[2], this.subdivisions, positions, indices, colors);
        
        const normals = [];
        BABYLON.VertexData.ComputeNormals(positions, indices, normals);
        
        const mesh = new BABYLON.Mesh("chunk_" + def.id, this.scene);
        const vertexData = new BABYLON.VertexData();
        vertexData.positions = positions;
        vertexData.indices = indices;
        vertexData.normals = normals;
        vertexData.colors = colors;
        vertexData.applyToMesh(mesh);
        
        mesh.material = this.terrainMat;
        mesh.useVertexColors = true;
        mesh.hasVertexAlpha = false;
        mesh.parent = this.worldContainer;
        mesh.isVisible = false;
        
        // Create trees container for this chunk
        const treesContainer = new BABYLON.TransformNode("trees_" + def.id, this.scene);
        treesContainer.parent = this.worldContainer;
        
        // Add trees to grass areas
        this.addTreesToChunk(def, treesContainer);
        
        this.chunks.set(def.id, { mesh, def, treesContainer });
        return this.chunks.get(def.id);
    }
    
    addTreesToChunk(def, container) {
        const v0 = def.vertices[0];
        const v1 = def.vertices[1];
        const v2 = def.vertices[2];
        
        // Sample points within the triangle
        for (let i = 1; i <= 4; i++) {
            for (let j = 1; j <= 4 - i; j++) {
                const k = 5 - i - j;
                const u = i / 5, v = j / 5, w = k / 5;
                const point = v0.scale(u).add(v1.scale(v)).add(v2.scale(w)).normalize();
                
                const h = this.getTerrainHeight(point);
                const normalizedH = ((h - this.radius) / (this.radius * 0.02) + 1) / 2;
                
                // Only place trees on grass (0.35 - 0.65)
                if (normalizedH >= 0.38 && normalizedH < 0.62) {
                    const treeVal = this.treeNoise.noise3D(point.x * 20, point.y * 20, point.z * 20);
                    if (treeVal > 0.3) {
                        this.createTree(point.scale(h), point, container, def.id * 100 + i * 10 + j);
                    }
                }
            }
        }
    }
    
    createTree(position, surfaceNormal, parent, id) {
        const tree = new BABYLON.TransformNode("tree_" + id, this.scene);
        tree.parent = parent;
        
        // Random variation
        const variation = Math.abs((position.x * 12345.67) % 1);
        const trunkHeight = 15 + variation * 10;
        const leavesSize = 12 + variation * 8;
        
        // Trunk
        const trunk = BABYLON.MeshBuilder.CreateCylinder("trunk_" + id, {
            height: trunkHeight,
            diameterTop: 1.5,
            diameterBottom: 2.5,
            tessellation: 8
        }, this.scene);
        trunk.material = this.trunkMat;
        trunk.parent = tree;
        trunk.position.y = trunkHeight / 2;
        
        // Leaves (cone shape)
        const leaves = BABYLON.MeshBuilder.CreateCylinder("leaves_" + id, {
            height: leavesSize,
            diameterTop: 0,
            diameterBottom: leavesSize * 0.8,
            tessellation: 8
        }, this.scene);
        leaves.material = this.leavesMat;
        leaves.parent = tree;
        leaves.position.y = trunkHeight + leavesSize / 2 - 2;
        
        // Position tree at surface
        tree.position.copyFrom(position);
        
        // Orient tree to stand perpendicular to surface
        const up = surfaceNormal.normalize();
        const rotAxis = BABYLON.Vector3.Cross(BABYLON.Vector3.Up(), up);
        if (rotAxis.length() > 0.001) {
            rotAxis.normalize();
            const angle = Math.acos(Math.max(-1, Math.min(1, BABYLON.Vector3.Dot(BABYLON.Vector3.Up(), up))));
            tree.rotationQuaternion = BABYLON.Quaternion.RotationAxis(rotAxis, angle);
        }
    }
    
    buildTriangle(v0, v1, v2, depth, positions, indices, colors) {
        if (depth === 0) {
            const baseIdx = positions.length / 3;
            [v0, v1, v2].forEach(v => {
                const dir = v.normalize();
                const h = this.getTerrainHeight(dir);
                const pos = dir.scale(h);
                positions.push(pos.x, pos.y, pos.z);
                
                const normalizedH = ((h - this.radius) / (this.radius * 0.02) + 1) / 2;
                const color = this.getTerrainColor(normalizedH);
                colors.push(color.r, color.g, color.b, 1);
            });
            indices.push(baseIdx, baseIdx + 1, baseIdx + 2);
            return;
        }
        const mid = (a, b) => a.add(b).scale(0.5).normalize();
        const m01 = mid(v0, v1), m12 = mid(v1, v2), m20 = mid(v2, v0);
        this.buildTriangle(v0, m01, m20, depth - 1, positions, indices, colors);
        this.buildTriangle(v1, m12, m01, depth - 1, positions, indices, colors);
        this.buildTriangle(v2, m20, m12, depth - 1, positions, indices, colors);
        this.buildTriangle(m01, m12, m20, depth - 1, positions, indices, colors);
    }
    
    updateVisibility(carDirection, viewAngle = 0.7) {
        const newActive = new Set();
        const carDir = carDirection.normalize();
        
        this.chunkDefinitions.forEach(def => {
            const dot = BABYLON.Vector3.Dot(carDir, def.center);
            if (dot > Math.cos(viewAngle)) {
                newActive.add(def.id);
                const chunk = this.buildChunk(def);
                chunk.mesh.isVisible = true;
                if (chunk.treesContainer) chunk.treesContainer.setEnabled(true);
            }
        });
        
        this.activeChunks.forEach(id => {
            if (!newActive.has(id)) {
                const chunk = this.chunks.get(id);
                if (chunk) {
                    chunk.mesh.isVisible = false;
                    if (chunk.treesContainer) chunk.treesContainer.setEnabled(false);
                }
            }
        });
        
        this.activeChunks = newActive;
    }
    
    orientWorld(carDirection, carHeading, terrainHeight) {
        const carDir = carDirection.normalize();
        const up = BABYLON.Vector3.Up();
        
        const rotAxis = BABYLON.Vector3.Cross(carDir, up);
        if (rotAxis.length() > 0.001) {
            rotAxis.normalize();
            const angle = Math.acos(Math.max(-1, Math.min(1, BABYLON.Vector3.Dot(carDir, up))));
            const alignQuat = BABYLON.Quaternion.RotationAxis(rotAxis, angle);
            const headingQuat = BABYLON.Quaternion.RotationAxis(up, -carHeading);
            this.worldContainer.rotationQuaternion = headingQuat.multiply(alignQuat);
        } else {
            this.worldContainer.rotationQuaternion = BABYLON.Quaternion.RotationAxis(up, -carHeading);
        }
        
        this.worldContainer.position.y = -terrainHeight;
    }
}

// ========================================
// Car Controller - Simplified approach
// ========================================
class CarController {
    constructor(carMesh, planet, camera) {
        this.mesh = carMesh;
        this.planet = planet;
        this.camera = camera;
        
        // Position on sphere using a direction vector instead of spherical coords
        this.position = new BABYLON.Vector3(1, 0, 0).normalize();  // Start on +X axis
        this.forward = new BABYLON.Vector3(0, 0, -1);  // Initial forward direction
        
        this.velocity = 0;
        this.maxSpeed = 0.0008;
        this.acceleration = 0.00002;
        this.brakeForce = 0.00004;
        this.friction = 0.98;
        this.turnSpeed = 0.03;
        this.steeringAngle = 0;
        this.maxSteering = 1.0;
    }
    
    update() {
        const input = getInput();
        
        // Smooth steering
        const targetSteer = input.turn * this.maxSteering;
        this.steeringAngle += (targetSteer - this.steeringAngle) * 0.15;
        
        // Acceleration / braking
        if (input.forward > 0) {
            this.velocity = Math.min(this.velocity + this.acceleration, this.maxSpeed);
        } else if (input.forward < 0) {
            if (this.velocity > 0.00005) {
                this.velocity = Math.max(this.velocity - this.brakeForce, 0);
            } else {
                this.velocity = Math.max(this.velocity - this.acceleration * 0.5, -this.maxSpeed * 0.4);
            }
        } else {
            this.velocity *= this.friction;
            if (Math.abs(this.velocity) < 0.000001) this.velocity = 0;
        }
        
        // Get current "up" direction (normal to sphere at car position)
        const up = this.position.normalize();
        
        // Turn: rotate forward direction around up axis
        if (Math.abs(this.velocity) > 0.000001) {
            const turnAmount = this.steeringAngle * this.turnSpeed;
            const turnDir = this.velocity >= 0 ? 1 : -1;
            const rotQuat = BABYLON.Quaternion.RotationAxis(up, turnAmount * turnDir);
            this.forward = this.forward.applyRotationQuaternion(rotQuat);
        }
        
        // Ensure forward is tangent to sphere (perpendicular to up)
        this.forward = this.forward.subtract(up.scale(BABYLON.Vector3.Dot(this.forward, up))).normalize();
        
        // Move along sphere surface
        if (Math.abs(this.velocity) > 0.0000001) {
            // Move position along the forward direction on the sphere
            const moveDir = this.forward.scale(this.velocity);
            this.position = this.position.add(moveDir).normalize();
            
            // Re-orient forward to be tangent at new position
            const newUp = this.position.normalize();
            this.forward = this.forward.subtract(newUp.scale(BABYLON.Vector3.Dot(this.forward, newUp))).normalize();
        }
        
        // Get terrain height at current position
        const terrainH = this.planet.getTerrainHeight(this.position);
        
        // Orient the world so car position is at "up" and car faces -Z
        this.orientWorldToPosition(terrainH);
        this.planet.updateVisibility(this.position);
        
        // Car stays at fixed screen position
        this.mesh.position.set(0, 5, 0);
        this.mesh.rotation.set(0, 0, 0);
    }
    
    orientWorldToPosition(terrainHeight) {
        const up = this.position.normalize();
        const worldUp = BABYLON.Vector3.Up();
        
        // Step 1: Rotate world so car's sphere position aligns with world UP
        let alignQuat = BABYLON.Quaternion.Identity();
        const rotAxis = BABYLON.Vector3.Cross(up, worldUp);
        if (rotAxis.length() > 0.001) {
            rotAxis.normalize();
            const angle = Math.acos(Math.max(-1, Math.min(1, BABYLON.Vector3.Dot(up, worldUp))));
            alignQuat = BABYLON.Quaternion.RotationAxis(rotAxis, angle);
        }
        
        // Step 2: Calculate where forward ends up after alignment
        const alignedForward = this.forward.applyRotationQuaternion(alignQuat);
        
        // Step 3: Rotate around Y to make forward point to -Z
        const targetForward = new BABYLON.Vector3(0, 0, -1);
        const flatForward = new BABYLON.Vector3(alignedForward.x, 0, alignedForward.z).normalize();
        
        let headingAngle = Math.atan2(flatForward.x, -flatForward.z);
        const headingQuat = BABYLON.Quaternion.RotationAxis(worldUp, headingAngle);
        
        // Combined rotation
        this.planet.worldContainer.rotationQuaternion = headingQuat.multiply(alignQuat);
        this.planet.worldContainer.position.y = -terrainHeight;
    }
}

// ========================================
// Car Mesh Builder
// ========================================
function createCarMesh(scene) {
    const car = new BABYLON.TransformNode("car", scene);
    
    // Car faces -Z direction (front is at negative Z)
    // Body
    const body = BABYLON.MeshBuilder.CreateBox("body", { width: 4, height: 1.5, depth: 8 }, scene);
    body.position.y = 1.5;
    body.parent = car;
    const bodyMat = new BABYLON.StandardMaterial("bodyMat", scene);
    bodyMat.diffuseColor = new BABYLON.Color3(0.9, 0.1, 0.1);
    bodyMat.specularColor = new BABYLON.Color3(0.3, 0.3, 0.3);
    body.material = bodyMat;
    
    // Cabin (toward back, so positive Z)
    const cabin = BABYLON.MeshBuilder.CreateBox("cabin", { width: 3.5, height: 1.2, depth: 3.5 }, scene);
    cabin.position.set(0, 2.8, 1);  // Cabin toward back (+Z)
    cabin.parent = car;
    const cabinMat = new BABYLON.StandardMaterial("cabinMat", scene);
    cabinMat.diffuseColor = new BABYLON.Color3(0.15, 0.15, 0.25);
    cabinMat.specularColor = new BABYLON.Color3(0.4, 0.4, 0.4);
    cabin.material = cabinMat;
    
    // Hood (front, so negative Z)
    const hood = BABYLON.MeshBuilder.CreateBox("hood", { width: 3.8, height: 0.8, depth: 2.5 }, scene);
    hood.position.set(0, 2.2, -2.5);  // Hood at front (-Z)
    hood.parent = car;
    const hoodMat = new BABYLON.StandardMaterial("hoodMat", scene);
    hoodMat.diffuseColor = new BABYLON.Color3(0.85, 0.08, 0.08);
    hood.material = hoodMat;
    
    // Wheels
    const wheelMat = new BABYLON.StandardMaterial("wheelMat", scene);
    wheelMat.diffuseColor = new BABYLON.Color3(0.15, 0.15, 0.15);
    
    // Front wheels at -Z, back wheels at +Z
    const wheelPositions = [
        { x: -2.2, z: -2.5 },  // Front left
        { x: 2.2, z: -2.5 },   // Front right
        { x: -2.2, z: 2.5 },   // Back left
        { x: 2.2, z: 2.5 }     // Back right
    ];
    
    wheelPositions.forEach(({ x, z }, i) => {
        const wheel = BABYLON.MeshBuilder.CreateCylinder("wheel" + i, { diameter: 1.4, height: 0.5 }, scene);
        wheel.rotation.z = Math.PI / 2;
        wheel.position.set(x, 0.7, z);
        wheel.parent = car;
        wheel.material = wheelMat;
    });
    
    // Headlights at front (-Z)
    const lightMat = new BABYLON.StandardMaterial("lightMat", scene);
    lightMat.emissiveColor = new BABYLON.Color3(1, 1, 0.7);
    
    [[-1.2, -4], [1.2, -4]].forEach(([x, z], i) => {
        const hl = BABYLON.MeshBuilder.CreateBox("headlight" + i, { width: 0.6, height: 0.4, depth: 0.1 }, scene);
        hl.position.set(x, 1.3, z);
        hl.parent = car;
        hl.material = lightMat;
    });
    
    // Taillights at back (+Z)
    const tailMat = new BABYLON.StandardMaterial("tailMat", scene);
    tailMat.emissiveColor = new BABYLON.Color3(1, 0.1, 0.1);
    
    [[-1.5, 4], [1.5, 4]].forEach(([x, z], i) => {
        const tl = BABYLON.MeshBuilder.CreateBox("taillight" + i, { width: 0.5, height: 0.3, depth: 0.1 }, scene);
        tl.position.set(x, 1.3, z);
        tl.parent = car;
        tl.material = tailMat;
    });
    
    return car;
}

// ========================================
// Game State
// ========================================
let gameState = null;

// ========================================
// Exported Functions for Blazor
// ========================================
export function initGame(canvasId) {
    console.log("initGame called with canvasId:", canvasId);
    
    try {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error("Canvas not found:", canvasId);
            return storeRef(null);
        }
        console.log("Canvas found");
        
        if (typeof BABYLON === 'undefined') {
            console.error("BABYLON is not defined!");
            return storeRef(null);
        }
        console.log("BABYLON version:", BABYLON.Engine.Version);
        
        const engine = new BABYLON.Engine(canvas, true);
        const scene = new BABYLON.Scene(engine);
        scene.clearColor = new BABYLON.Color4(0.4, 0.6, 0.9, 1);
        
        // Fixed camera behind and above the car
        // Car is at (0, 5, 0), facing -Z
        // Camera positioned at (0, 25, 60) looking at car
        const camera = new BABYLON.FreeCamera("camera", new BABYLON.Vector3(0, 25, 55), scene);
        camera.setTarget(new BABYLON.Vector3(0, 5, -10));  // Look ahead of car
        camera.minZ = 1;
        camera.maxZ = 200000;
        camera.fov = 0.9;
        // Don't attach controls - camera is fixed
        
        // Light from above
        const light = new BABYLON.HemisphericLight("light", new BABYLON.Vector3(0, 1, 0), scene);
        light.intensity = 1.2;
        light.groundColor = new BABYLON.Color3(0.3, 0.3, 0.4);
        
        // Add directional light for better shading
        const dirLight = new BABYLON.DirectionalLight("dirLight", new BABYLON.Vector3(-0.5, -1, -0.5), scene);
        dirLight.intensity = 0.5;
        
        const planet = new PlanetChunkSystem(scene, 5000, 5);
        console.log("Planet created with", planet.chunkDefinitions.length, "chunks");
        
        const car = createCarMesh(scene);
        console.log("Car created");
        
        const controller = new CarController(car, planet, camera);
        controller.update();
        
        scene.registerBeforeRender(() => controller.update());
        
        engine.runRenderLoop(() => scene.render());
        window.addEventListener('resize', () => engine.resize());
        
        canvas.focus();
        canvas.addEventListener('click', () => canvas.focus());
        
        gameState = { engine, scene, camera, planet, car, controller };
        console.log("Game initialized successfully!");
        
        return storeRef(gameState);
    } catch (error) {
        console.error("Error in initGame:", error);
        return storeRef(null);
    }
}

export function disposeGame(gameRef) {
    const game = getRef(gameRef);
    if (game && game.engine) {
        game.engine.dispose();
        gameState = null;
    }
}
