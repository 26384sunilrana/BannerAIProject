export interface Rect {
  x: number
  y: number
  width: number
  height: number
}

export interface Point {
  x: number
  y: number
}

export function screenToCanvas(
  screenX: number,
  screenY: number,
  canvasRect: DOMRect,
  scale: number
): Point {
  return {
    x: (screenX - canvasRect.left) / scale,
    y: (screenY - canvasRect.top) / scale,
  }
}

export function canvasToScreen(
  canvasX: number,
  canvasY: number,
  canvasRect: DOMRect,
  scale: number
): Point {
  return {
    x: canvasX * scale + canvasRect.left,
    y: canvasY * scale + canvasRect.top,
  }
}

export function clampPosition(
  point: Point,
  componentSize: { width: number; height: number },
  canvasSize: { width: number; height: number }
): Point {
  return {
    x: Math.max(0, Math.min(point.x, canvasSize.width - componentSize.width)),
    y: Math.max(0, Math.min(point.y, canvasSize.height - componentSize.height)),
  }
}

export function snapToGrid(value: number, gridSize: number): number {
  return Math.round(value / gridSize) * gridSize
}

export function getDistance(p1: Point, p2: Point): number {
  const dx = p2.x - p1.x
  const dy = p2.y - p1.y
  return Math.sqrt(dx * dx + dy * dy)
}

export function isPointInRect(point: Point, rect: Rect): boolean {
  return (
    point.x >= rect.x &&
    point.x <= rect.x + rect.width &&
    point.y >= rect.y &&
    point.y <= rect.y + rect.height
  )
}

export function rectsIntersect(rect1: Rect, rect2: Rect): boolean {
  return !(
    rect1.x + rect1.width <= rect2.x ||
    rect2.x + rect2.width <= rect1.x ||
    rect1.y + rect1.height <= rect2.y ||
    rect2.y + rect2.height <= rect1.y
  )
}
