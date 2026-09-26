export interface ValidationResult {
  valid: boolean
  error?: string
}

export const validation = {
  isValidComponentPosition(x: number, y: number): ValidationResult {
    if (!Number.isFinite(x) || !Number.isFinite(y)) {
      return { valid: false, error: 'Position must be a valid number' }
    }
    if (x < 0 || y < 0) {
      return { valid: false, error: 'Position cannot be negative' }
    }
    return { valid: true }
  },

  isValidComponentSize(width: number, height: number): ValidationResult {
    const MIN_SIZE = 10
    const MAX_SIZE = 10000

    if (!Number.isFinite(width) || !Number.isFinite(height)) {
      return { valid: false, error: 'Size must be a valid number' }
    }
    if (width < MIN_SIZE || height < MIN_SIZE) {
      return { valid: false, error: `Minimum size is ${MIN_SIZE}px` }
    }
    if (width > MAX_SIZE || height > MAX_SIZE) {
      return { valid: false, error: `Maximum size is ${MAX_SIZE}px` }
    }
    return { valid: true }
  },

  isValidZIndex(zIndex: number): ValidationResult {
    if (!Number.isInteger(zIndex)) {
      return { valid: false, error: 'Z-index must be an integer' }
    }
    if (zIndex < 0 || zIndex > 100) {
      return { valid: false, error: 'Z-index must be between 0 and 100' }
    }
    return { valid: true }
  },

  isValidRotation(rotation: number): ValidationResult {
    if (!Number.isFinite(rotation)) {
      return { valid: false, error: 'Rotation must be a valid number' }
    }
    return { valid: true }
  },

  isValidOpacity(opacity: number): ValidationResult {
    if (!Number.isFinite(opacity)) {
      return { valid: false, error: 'Opacity must be a valid number' }
    }
    if (opacity < 0 || opacity > 1) {
      return { valid: false, error: 'Opacity must be between 0 and 1' }
    }
    return { valid: true }
  },

  isValidText(text: string): ValidationResult {
    if (typeof text !== 'string') {
      return { valid: false, error: 'Text must be a string' }
    }
    if (text.length > 5000) {
      return { valid: false, error: 'Text must not exceed 5000 characters' }
    }
    return { valid: true }
  },

  isValidFontSize(size: number): ValidationResult {
    const MIN_SIZE = 8
    const MAX_SIZE = 200

    if (!Number.isFinite(size)) {
      return { valid: false, error: 'Font size must be a valid number' }
    }
    if (size < MIN_SIZE || size > MAX_SIZE) {
      return { valid: false, error: `Font size must be between ${MIN_SIZE}px and ${MAX_SIZE}px` }
    }
    return { valid: true }
  },

  isValidColor(color: string): ValidationResult {
    const hexRegex = /^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/
    const rgbRegex = /^rgb\(\d{1,3},\s*\d{1,3},\s*\d{1,3}\)$/
    const rgbaRegex = /^rgba\(\d{1,3},\s*\d{1,3},\s*\d{1,3},\s*[0-1](?:\.\d+)?\)$/

    if (!hexRegex.test(color) && !rgbRegex.test(color) && !rgbaRegex.test(color)) {
      return { valid: false, error: 'Invalid color format' }
    }
    return { valid: true }
  },

  isValidUrl(url: string): ValidationResult {
    try {
      new URL(url)
      return { valid: true }
    } catch {
      return { valid: false, error: 'Invalid URL' }
    }
  },

  isValidBannerId(bannerId: string): ValidationResult {
    if (!bannerId || typeof bannerId !== 'string') {
      return { valid: false, error: 'Banner ID is required' }
    }
    if (bannerId.length > 100) {
      return { valid: false, error: 'Banner ID is too long' }
    }
    return { valid: true }
  },
}
