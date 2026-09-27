import { validation } from '@/utils/validation'

describe('validation utilities', () => {
  describe('isValidComponentPosition', () => {
    it('accepts valid positions', () => {
      const result = validation.isValidComponentPosition(0, 0)
      expect(result.valid).toBe(true)
    })

    it('rejects negative positions', () => {
      const result = validation.isValidComponentPosition(-10, 0)
      expect(result.valid).toBe(false)
      expect(result.error).toContain('negative')
    })

    it('rejects non-numeric values', () => {
      const result = validation.isValidComponentPosition(NaN, 0)
      expect(result.valid).toBe(false)
    })
  })

  describe('isValidComponentSize', () => {
    it('accepts valid sizes', () => {
      const result = validation.isValidComponentSize(100, 100)
      expect(result.valid).toBe(true)
    })

    it('rejects sizes below minimum', () => {
      const result = validation.isValidComponentSize(5, 100)
      expect(result.valid).toBe(false)
      expect(result.error).toContain('Minimum')
    })

    it('rejects sizes above maximum', () => {
      const result = validation.isValidComponentSize(11000, 100)
      expect(result.valid).toBe(false)
      expect(result.error).toContain('Maximum')
    })
  })

  describe('isValidZIndex', () => {
    it('accepts valid z-index', () => {
      const result = validation.isValidZIndex(50)
      expect(result.valid).toBe(true)
    })

    it('rejects negative z-index', () => {
      const result = validation.isValidZIndex(-1)
      expect(result.valid).toBe(false)
    })

    it('rejects z-index above maximum', () => {
      const result = validation.isValidZIndex(101)
      expect(result.valid).toBe(false)
    })

    it('rejects non-integer z-index', () => {
      const result = validation.isValidZIndex(50.5)
      expect(result.valid).toBe(false)
    })
  })

  describe('isValidOpacity', () => {
    it('accepts values between 0 and 1', () => {
      expect(validation.isValidOpacity(0).valid).toBe(true)
      expect(validation.isValidOpacity(0.5).valid).toBe(true)
      expect(validation.isValidOpacity(1).valid).toBe(true)
    })

    it('rejects values outside range', () => {
      const resultLow = validation.isValidOpacity(-0.1)
      expect(resultLow.valid).toBe(false)

      const resultHigh = validation.isValidOpacity(1.1)
      expect(resultHigh.valid).toBe(false)
    })
  })

  describe('isValidFontSize', () => {
    it('accepts valid font sizes', () => {
      const result = validation.isValidFontSize(16)
      expect(result.valid).toBe(true)
    })

    it('rejects sizes below minimum', () => {
      const result = validation.isValidFontSize(5)
      expect(result.valid).toBe(false)
    })

    it('rejects sizes above maximum', () => {
      const result = validation.isValidFontSize(250)
      expect(result.valid).toBe(false)
    })
  })

  describe('isValidColor', () => {
    it('accepts hex colors', () => {
      expect(validation.isValidColor('#000000').valid).toBe(true)
      expect(validation.isValidColor('#FFF').valid).toBe(true)
    })

    it('accepts rgb colors', () => {
      const result = validation.isValidColor('rgb(255, 0, 0)')
      expect(result.valid).toBe(true)
    })

    it('accepts rgba colors', () => {
      const result = validation.isValidColor('rgba(255, 0, 0, 0.5)')
      expect(result.valid).toBe(true)
    })

    it('rejects invalid colors', () => {
      const result = validation.isValidColor('notacolor')
      expect(result.valid).toBe(false)
    })
  })

  describe('isValidUrl', () => {
    it('accepts valid URLs', () => {
      expect(validation.isValidUrl('http://example.com').valid).toBe(true)
      expect(validation.isValidUrl('https://example.com/path').valid).toBe(true)
    })

    it('rejects invalid URLs', () => {
      const result = validation.isValidUrl('not a url')
      expect(result.valid).toBe(false)
    })
  })

  describe('isValidBannerId', () => {
    it('accepts valid banner IDs', () => {
      const result = validation.isValidBannerId('123')
      expect(result.valid).toBe(true)
    })

    it('rejects empty IDs', () => {
      const result = validation.isValidBannerId('')
      expect(result.valid).toBe(false)
    })

    it('rejects IDs that are too long', () => {
      const longId = 'a'.repeat(101)
      const result = validation.isValidBannerId(longId)
      expect(result.valid).toBe(false)
    })
  })
})
