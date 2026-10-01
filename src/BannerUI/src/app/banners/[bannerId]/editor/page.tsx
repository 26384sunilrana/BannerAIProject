'use client'

import { useEffect } from 'react'
import { useParams } from 'next/navigation'
import { EditorProvider } from '@/context/EditorContext'
import { useEditor } from '@/hooks/useEditor'
import { useSave } from '@/hooks/useSave'
import { useToast } from '@/hooks/useToast'
import { bannerService } from '@/api/bannerService'
import { Header, Toolbar, Canvas, PropertyPanel, Toast } from '@/components'

function EditorContent() {
  const { bannerId } = useParams()
  const { state, setBanner, setError, selectComponent, addComponent, updateComponent, deleteComponent } = useEditor()
  const save = useSave(typeof bannerId === 'string' ? bannerId : '', state.components, state.banner)
  const toast = useToast()

  useEffect(() => {
    async function loadBanner() {
      try {
        if (typeof bannerId === 'string') {
          const banner = await bannerService.getBanner(bannerId)
          setBanner(banner)
          toast.success('Banner loaded')
        }
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Failed to load banner'
        setError(message)
        toast.error(message)
      }
    }

    loadBanner()
  }, [bannerId, setBanner, setError, toast])

  const handleAddComponent = async (type: any) => {
    if (!state.banner) return

    try {
      const newComponent = await bannerService.addComponent(state.banner.id, {
        type,
        x: 50,
        y: 50,
        width: 200,
        height: 100,
        zIndex: state.components.length,
        data: getDefaultDataForType(type),
      })

      addComponent(newComponent)
      selectComponent(newComponent.id)
      save.markDirty()
      toast.success(`${type} component added`)
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to add component'
      toast.error(message)
    }
  }

  const handleComponentMove = async (componentId: string, x: number, y: number) => {
    updateComponent(componentId, { x, y })
    save.markDirty()
  }

  const handleComponentResize = async (componentId: string, width: number, height: number) => {
    updateComponent(componentId, { width, height })
    save.markDirty()
  }

  const handlePropertyChange = (property: string, value: any) => {
    if (!state.selectedComponentId) return
    updateComponent(state.selectedComponentId, { [property]: value })
    save.markDirty()
  }

  const handleDeleteComponent = async () => {
    if (!state.selectedComponentId || !state.banner) return

    try {
      await bannerService.deleteComponent(state.banner.id, state.selectedComponentId)
      deleteComponent(state.selectedComponentId)
      selectComponent(null)
      save.markDirty()
      toast.success('Component deleted')
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to delete component'
      toast.error(message)
    }
  }

  const handleSave = async () => {
    try {
      await save.save()
      toast.success('Banner saved successfully')
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to save banner'
      toast.error(message)
    }
  }

  if (state.isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading banner...</p>
        </div>
      </div>
    )
  }

  if (state.error) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-center">
          <p className="text-red-600 mb-4">Error: {state.error}</p>
          <button
            onClick={() => window.location.reload()}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Retry
          </button>
        </div>
      </div>
    )
  }

  const selectedComponent = state.components.find((c) => c.id === state.selectedComponentId)

  return (
    <div className="flex flex-col h-screen bg-gray-100">
      <Header
        bannerId={typeof bannerId === 'string' ? bannerId : 'unknown'}
        onSave={handleSave}
        onUndo={() => {}}
        onRedo={() => {}}
        onTogglePreview={() => {}}
        isSaving={save.isSaving}
        isDirty={save.isDirty}
        canUndo={false}
        canRedo={false}
        isPreviewMode={state.isPreviewMode}
      />

      <div className="flex flex-1 overflow-hidden">
        <Toolbar
          onAddComponent={handleAddComponent}
          maxComponents={50}
          currentComponentCount={state.components.length}
        />

        <Canvas
          components={state.components}
          backgroundColor={state.banner?.backgroundColor || '#ffffff'}
          width={state.banner?.width || 1200}
          height={state.banner?.height || 600}
          isPreviewMode={state.isPreviewMode}
          selectedComponentId={state.selectedComponentId}
          onComponentSelect={selectComponent}
          onComponentMove={handleComponentMove}
          onComponentResize={handleComponentResize}
        />

        <PropertyPanel
          selectedComponent={selectedComponent || null}
          onPropertyChange={handlePropertyChange}
          onDeleteComponent={handleDeleteComponent}
        />
      </div>

      <Toast messages={toast.messages} onRemove={toast.remove} />
    </div>
  )
}

function getDefaultDataForType(type: string) {
  switch (type) {
    case 'text':
      return {
        content: 'New Text',
        fontSize: 24,
        fontFamily: 'Arial',
        fontWeight: '400',
        color: '#000000',
        textAlign: 'left',
        lineHeight: 1.5,
      }
    case 'image':
      return {
        mediaFileId: '',
        mediaUrl: '',
        alt: 'Image',
        objectFit: 'cover',
      }
    case 'video':
      return {
        mediaFileId: '',
        mediaUrl: '',
        poster: '',
        autoPlay: false,
        loop: false,
        muted: false,
        duration: 0,
        resolution: '1080p',
      }
    case 'graphics':
      return {
        shapeType: 'rectangle',
        fillColor: '#3b82f6',
        strokeColor: '#000000',
        strokeWidth: 1,
      }
    default:
      return {}
  }
}

export default function EditorPage() {
  return (
    <EditorProvider>
      <EditorContent />
    </EditorProvider>
  )
}
