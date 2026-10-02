import {
  screenToCanvas,
  canvasToScreen,
  snapToGrid,
  getDistance,
  isPointInRect,
  rectsIntersect,
} from '@/utils/positioning'

describe('positioning utilities', () => {
  const canvasRect = new DOMRect(100, 100, 1200, 600)

  describe('screenToCanvas', () => {
    it('converts screen coordinates to canvas coordinates', () => {
      const result = screenToCanvas(150, 150, canvasRect, 1)
      expect(result.x).toBe(50)
      expect(result.y).toBe(50)
    })

    it('accounts for scale factor', () => {
      const result = screenToCanvas(200, 200, canvasRect, 2)
      expect(result.x).toBe(50)
      expect(result.y).toBe(50)
    })
  })

  describe('canvasToScreen', () => {
    it('converts canvas coordinates to screen coordinates', () => {
      const result = canvasToScreen(50, 50, canvasRect, 1)
      expect(result.x).toBe(150)
      expect(result.y).toBe(150)
    })

    it('accounts for scale factor', () => {
      const result = canvasToScreen(50, 50, canvasRect, 2)
      expect(result.x).toBe(200)
      expect(result.y).toBe(200)
    })
  })

  describe('snapToGrid', () => {
    it('snaps values to grid', () => {
      expect(snapToGrid(13, 8)).toBe(16)
      expect(snapToGrid(20, 8)).toBe(24)
      expect(snapToGrid(25, 8)).toBe(24)
    })

    it('handles zero and multiples', () => {
      expect(snapToGrid(0, 8)).toBe(0)
      expect(snapToGrid(8, 8)).toBe(8)
      expect(snapToGrid(16, 8)).toBe(16)
    })
  })

  describe('getDistance', () => {
    it('calculates distance between two points', () => {
      const distance = getDistance({ x: 0, y: 0 }, { x: 3, y: 4 })
      expect(distance).toBe(5)
    })

    it('handles same point', () => {
      const distance = getDistance({ x: 5, y: 5 }, { x: 5, y: 5 })
      expect(distance).toBe(0)
    })
  })

  describe('isPointInRect', () => {
    const rect = { x: 10, y: 10, width: 100, height: 100 }

    it('detects point inside rectangle', () => {
      expect(isPointInRect({ x: 50, y: 50 }, rect)).toBe(true)
    })

    it('detects point on rectangle edge', () => {
      expect(isPointInRect({ x: 10, y: 10 }, rect)).toBe(true)
      expect(isPointInRect({ x: 110, y: 110 }, rect)).toBe(true)
    })

    it('detects point outside rectangle', () => {
      expect(isPointInRect({ x: 5, y: 5 }, rect)).toBe(false)
      expect(isPointInRect({ x: 150, y: 150 }, rect)).toBe(false)
    })
  })

  describe('rectsIntersect', () => {
    const rect1 = { x: 0, y: 0, width: 100, height: 100 }

    it('detects intersecting rectangles', () => {
      const rect2 = { x: 50, y: 50, width: 100, height: 100 }
      expect(rectsIntersect(rect1, rect2)).toBe(true)
    })

    it('detects non-intersecting rectangles', () => {
      const rect2 = { x: 200, y: 200, width: 100, height: 100 }
      expect(rectsIntersect(rect1, rect2)).toBe(false)
    })

    it('detects touching rectangles', () => {
      const rect2 = { x: 100, y: 0, width: 100, height: 100 }
      expect(rectsIntersect(rect1, rect2)).toBe(false)
    })
  })
})
