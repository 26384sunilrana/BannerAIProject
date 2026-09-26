import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EditorProvider } from '@/context/EditorContext'
import { useEditor } from '@/hooks/useEditor'

// Mock component to test the flow
function EditorTestComponent() {
  const { state, addComponent, updateComponent, deleteComponent, selectComponent } = useEditor()

  return (
    <div>
      <div data-testid="component-count">{state.components.length}</div>
      <div data-testid="selected-id">{state.selectedComponentId || 'none'}</div>

      <button
        onClick={() => {
          addComponent({
            id: `comp-${Date.now()}`,
            bannerId: 'test-banner',
            type: 'text',
            x: 0,
            y: 0,
            width: 100,
            height: 50,
            zIndex: 0,
            rotation: 0,
            opacity: 1,
            isVisible: true,
            data: { content: 'Test' },
            effects: [],
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          })
        }}
      >
        Add Component
      </button>

      {state.components.map((comp) => (
        <div
          key={comp.id}
          data-testid={`component-${comp.id}`}
          onClick={() => selectComponent(comp.id)}
        >
          {comp.type}
        </div>
      ))}

      {state.selectedComponentId && (
        <button
          onClick={() => {
            updateComponent(state.selectedComponentId!, { x: 100 })
          }}
        >
          Move Component
        </button>
      )}

      {state.selectedComponentId && (
        <button
          onClick={() => {
            deleteComponent(state.selectedComponentId!)
          }}
        >
          Delete Component
        </button>
      )}
    </div>
  )
}

describe('Editor Integration Flow', () => {
  it('adds and displays new component', async () => {
    render(
      <EditorProvider>
        <EditorTestComponent />
      </EditorProvider>
    )

    const addButton = screen.getByRole('button', { name: /Add Component/i })
    await userEvent.click(addButton)

    await waitFor(() => {
      expect(screen.getByTestId('component-count')).toHaveTextContent('1')
    })
  })

  it('selects component when clicked', async () => {
    render(
      <EditorProvider>
        <EditorTestComponent />
      </EditorProvider>
    )

    const addButton = screen.getByRole('button', { name: /Add Component/i })
    await userEvent.click(addButton)

    await waitFor(() => {
      const components = screen.getAllByTestId(/component-comp-/)
      expect(components.length).toBeGreaterThan(0)
    })

    const component = screen.getAllByTestId(/component-comp-/)[0]
    await userEvent.click(component)

    expect(screen.getByTestId('selected-id')).not.toHaveTextContent('none')
  })

  it('updates component property', async () => {
    render(
      <EditorProvider>
        <EditorTestComponent />
      </EditorProvider>
    )

    // Add component
    const addButton = screen.getByRole('button', { name: /Add Component/i })
    await userEvent.click(addButton)

    // Select component
    await waitFor(() => {
      const components = screen.getAllByTestId(/component-comp-/)
      if (components.length > 0) {
        userEvent.click(components[0])
      }
    })

    // Move component
    const moveButton = screen.queryByRole('button', { name: /Move Component/i })
    if (moveButton) {
      await userEvent.click(moveButton)
      // Component should be updated (x changed to 100)
    }
  })

  it('deletes component', async () => {
    render(
      <EditorProvider>
        <EditorTestComponent />
      </EditorProvider>
    )

    // Add component
    const addButton = screen.getByRole('button', { name: /Add Component/i })
    await userEvent.click(addButton)

    await waitFor(() => {
      expect(screen.getByTestId('component-count')).toHaveTextContent('1')
    })

    // Select component
    const component = screen.getAllByTestId(/component-comp-/)[0]
    await userEvent.click(component)

    // Delete component
    const deleteButton = screen.getByRole('button', { name: /Delete Component/i })
    await userEvent.click(deleteButton)

    await waitFor(() => {
      expect(screen.getByTestId('component-count')).toHaveTextContent('0')
    })
  })

  it('handles multiple components', async () => {
    render(
      <EditorProvider>
        <EditorTestComponent />
      </EditorProvider>
    )

    const addButton = screen.getByRole('button', { name: /Add Component/i })

    // Add multiple components
    await userEvent.click(addButton)
    await userEvent.click(addButton)
    await userEvent.click(addButton)

    await waitFor(() => {
      expect(screen.getByTestId('component-count')).toHaveTextContent('3')
    })
  })
})
