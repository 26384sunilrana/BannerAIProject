import { renderHook, act } from '@testing-library/react'
import { useSelection } from '@/hooks/useSelection'

describe('useSelection', () => {
  it('initializes with no selection', () => {
    const { result } = renderHook(() => useSelection())
    expect(result.current.selectedComponentId).toBeNull()
    expect(result.current.multiSelectIds).toHaveLength(0)
  })

  it('selects a single component', () => {
    const { result } = renderHook(() => useSelection())

    act(() => {
      result.current.selectComponent('comp-1')
    })

    expect(result.current.selectedComponentId).toBe('comp-1')
    expect(result.current.isSelected('comp-1')).toBe(true)
    expect(result.current.isSelected('comp-2')).toBe(false)
  })

  it('deselects components', () => {
    const { result } = renderHook(() => useSelection())

    act(() => {
      result.current.selectComponent('comp-1')
    })

    act(() => {
      result.current.deselectComponent()
    })

    expect(result.current.selectedComponentId).toBeNull()
  })

  it('supports multi-select with Ctrl key', () => {
    const { result } = renderHook(() => useSelection())

    act(() => {
      result.current.toggleMultiSelect('comp-1', false)
    })

    act(() => {
      result.current.toggleMultiSelect('comp-2', true)
    })

    expect(result.current.selectedComponentId).toBe('comp-2')
    expect(result.current.isMultiSelected('comp-2')).toBe(true)
  })

  it('adds and removes from multi-select', () => {
    const { result } = renderHook(() => useSelection())

    act(() => {
      result.current.addToMultiSelect('comp-1')
    })

    expect(result.current.isMultiSelected('comp-1')).toBe(true)

    act(() => {
      result.current.removeFromMultiSelect('comp-1')
    })

    expect(result.current.isMultiSelected('comp-1')).toBe(false)
  })

  it('gets all selected components', () => {
    const { result } = renderHook(() => useSelection())

    act(() => {
      result.current.selectComponent('comp-1')
    })

    act(() => {
      result.current.addToMultiSelect('comp-2')
      result.current.addToMultiSelect('comp-3')
    })

    const allSelected = result.current.getAllSelected()
    expect(allSelected).toContain('comp-1')
    expect(allSelected).toContain('comp-2')
    expect(allSelected).toContain('comp-3')
    expect(allSelected).toHaveLength(3)
  })
})
