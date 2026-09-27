import { renderHook } from '@testing-library/react'
import { useEditor } from '@/hooks/useEditor'
import { EditorContext, EditorContextType } from '@/context/EditorContext'
import React from 'react'

describe('useEditor', () => {
  it('throws error when used outside EditorProvider', () => {
    // Suppress console.error for this test
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {})

    expect(() => {
      renderHook(() => useEditor())
    }).toThrow('useEditor must be used within EditorProvider')

    consoleSpy.mockRestore()
  })

  it('returns editor context when used within provider', () => {
    const mockContext: EditorContextType = {
      state: {
        banner: {
          id: '1',
          title: 'Test Banner',
          description: 'Test',
          width: 1200,
          height: 600,
          backgroundColor: '#fff',
        },
        components: [],
        selectedComponentId: null,
        isLoading: false,
        error: null,
        isPreviewMode: false,
      },
      setBanner: jest.fn(),
      setLoading: jest.fn(),
      setError: jest.fn(),
      addComponent: jest.fn(),
      updateComponent: jest.fn(),
      deleteComponent: jest.fn(),
      selectComponent: jest.fn(),
    }

    const wrapper = ({ children }: { children: React.ReactNode }) =>
      React.createElement(EditorContext.Provider, { value: mockContext }, children)

    const { result } = renderHook(() => useEditor(), { wrapper })

    expect(result.current).toEqual(mockContext)
  })

  it('provides access to editor state', () => {
    const mockContext: EditorContextType = {
      state: {
        banner: {
          id: '1',
          title: 'Test',
          description: '',
          width: 1200,
          height: 600,
          backgroundColor: '#fff',
        },
        components: [
          {
            id: 'comp1',
            type: 'text',
            x: 0,
            y: 0,
            width: 100,
            height: 50,
            zIndex: 0,
            rotation: 0,
            opacity: 1,
            isVisible: true,
            data: {},
          },
        ],
        selectedComponentId: 'comp1',
        isLoading: false,
        error: null,
        isPreviewMode: false,
      },
      setBanner: jest.fn(),
      setLoading: jest.fn(),
      setError: jest.fn(),
      addComponent: jest.fn(),
      updateComponent: jest.fn(),
      deleteComponent: jest.fn(),
      selectComponent: jest.fn(),
    }

    const wrapper = ({ children }: { children: React.ReactNode }) =>
      React.createElement(EditorContext.Provider, { value: mockContext }, children)

    const { result } = renderHook(() => useEditor(), { wrapper })

    expect(result.current.state.banner?.title).toBe('Test')
    expect(result.current.state.components).toHaveLength(1)
    expect(result.current.state.selectedComponentId).toBe('comp1')
  })

  it('provides setState functions', () => {
    const setBanner = jest.fn()
    const setLoading = jest.fn()
    const setError = jest.fn()
    const addComponent = jest.fn()
    const updateComponent = jest.fn()
    const deleteComponent = jest.fn()
    const selectComponent = jest.fn()

    const mockContext: EditorContextType = {
      state: {
        banner: null,
        components: [],
        selectedComponentId: null,
        isLoading: false,
        error: null,
        isPreviewMode: false,
      },
      setBanner,
      setLoading,
      setError,
      addComponent,
      updateComponent,
      deleteComponent,
      selectComponent,
    }

    const wrapper = ({ children }: { children: React.ReactNode }) =>
      React.createElement(EditorContext.Provider, { value: mockContext }, children)

    const { result } = renderHook(() => useEditor(), { wrapper })

    expect(result.current.setBanner).toBe(setBanner)
    expect(result.current.setLoading).toBe(setLoading)
    expect(result.current.setError).toBe(setError)
    expect(result.current.addComponent).toBe(addComponent)
    expect(result.current.updateComponent).toBe(updateComponent)
    expect(result.current.deleteComponent).toBe(deleteComponent)
    expect(result.current.selectComponent).toBe(selectComponent)
  })

  it('allows calling editor functions', () => {
    const selectComponent = jest.fn()
    const mockContext: EditorContextType = {
      state: {
        banner: null,
        components: [],
        selectedComponentId: null,
        isLoading: false,
        error: null,
        isPreviewMode: false,
      },
      setBanner: jest.fn(),
      setLoading: jest.fn(),
      setError: jest.fn(),
      addComponent: jest.fn(),
      updateComponent: jest.fn(),
      deleteComponent: jest.fn(),
      selectComponent,
    }

    const wrapper = ({ children }: { children: React.ReactNode }) =>
      React.createElement(EditorContext.Provider, { value: mockContext }, children)

    const { result } = renderHook(() => useEditor(), { wrapper })
    result.current.selectComponent('comp1')

    expect(selectComponent).toHaveBeenCalledWith('comp1')
  })
})
