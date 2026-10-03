import type { DeviceFace } from './device-components';

export type Orientation = { x: number; y: number };
export const faceOrientations: Record<DeviceFace, Orientation> = {
  frente: { x: 5, y: -15 }, tras: { x: 5, y: 165 },
  esquerda: { x: 5, y: 70 }, direita: { x: 5, y: -70 },
  superior: { x: -68, y: -14 }, inferior: { x: 68, y: -14 },
};

// Project each outward normal toward the camera after rotateX(x) rotateY(y).
export function visibleFaces({ x, y }: Orientation): Record<DeviceFace, number> {
  const rx = x * Math.PI / 180; const ry = y * Math.PI / 180;
  return {
    frente: Math.cos(rx) * Math.cos(ry), tras: -Math.cos(rx) * Math.cos(ry),
    esquerda: Math.cos(rx) * Math.sin(ry), direita: -Math.cos(rx) * Math.sin(ry),
    superior: -Math.sin(rx), inferior: Math.sin(rx),
  };
}
