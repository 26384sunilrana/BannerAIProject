import '@testing-library/jest-dom'

// jsdom has no DOMRect
if (typeof global.DOMRect === 'undefined') {
  global.DOMRect = class DOMRect {
    constructor(x = 0, y = 0, width = 0, height = 0) {
      this.x = x; this.y = y; this.width = width; this.height = height
      this.top = y; this.left = x; this.right = x + width; this.bottom = y + height
    }
    toJSON() { return this }
  }
}
