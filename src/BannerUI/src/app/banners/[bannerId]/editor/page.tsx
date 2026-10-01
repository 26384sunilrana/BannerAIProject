'use client'

import { useCallback, useEffect, useRef } from 'react'
import { useParams } from 'next/navigation'
import { EditorProvider } from '@/context/EditorContext'
import { useEditor } from '@/hooks/useEditor'
import { useSave } from '@/hooks/useSave'
import { useMediaUpload } from '@/hooks/useMediaUpload'
import { useToast } from '@/hooks/useToast'
import { bannerService } from '@/api/bannerService'
import { layerService } from '@/api/layerService'
import { nextFreeZIndex } from '@/api/bannerMapper'
import { getErrorMessage } from '@/api/client'
import { RequireAuth } from '@/components/auth/RequireAuth'
import { Roles } from '@/lib/session'
import { Header, Toolbar, Canvas, PropertyPanel, Toast } from '@/components'
import { ComponentType } from '@/types/banner'

/** Properties that belong to a component's content rather than to the component itself. */
const CONTENT_PROPERTIES = new Set([
  'content', 'fontSize', 'fontFamily', 'fontWeight', 'color', 'textAlign', 'lineHeight',
  'mediaFileId', 'mediaUrl', 'alt', 'objectFit',
  'poster', 'autoPlay', 'loop', 'muted',
  'shapeType', 'fillColor', 'strokeColor', 'strokeWidth',
])

function EditorContent() {
  const { bannerId } = useParams()
  const id = typeof bannerId === 'string' ? bannerId : ''
  const { state, setBanner, setError, setPreviewMode, selectComponent, addComponent, updateComponent, deleteComponent } = useEditor()
  const save = useSave(id, state.components, state.banner)
  const toast = useToast()
  const media = useMediaUpload()

  // The toast helpers change on every render; keep the latest in a ref so loading runs once per banner.
  const toastRef = useRef(toast)
  toastRef.current = toast

  const load = useCallback(async () => {
    const banner = await bannerService.getBanner(id)
    setBanner(banner)
    return banner
  }, [id, setBanner])

  useEffect(() => {
    if (!id) return
    load().catch((error) => {
      const message = getErrorMessage(error, 'Failed to load banner')
      setError(message)
      toastRef.current.error(message)
    })
  }, [id, load, setError])

  const handleAddComponent = async (type: ComponentType) => {
    if (!state.banner) return

    const zIndex = nextFreeZIndex(state.components.map((c) => c.zIndex))
    if (zIndex === null) {
      toast.error('This banner has no free layer left.')
      return
    }

    try {
      const newComponent = await bannerService.addComponent(state.banner.id, {
        type,
        x: 50,
        y: 50,
        width: 200,
        height: 100,
        zIndex,
        data: getDefaultDataForType(type),
      })

      addComponent(newComponent)
      selectComponent(newComponent.id)
      toast.success(`${type} component added`)
    } catch (error) {
      toast.error(getErrorMessage(error, 'Failed to add component'))
    }
  }

  const handleComponentMove = async (componentId: string, x: number, y: number) => {
    updateComponent(componentId, { x: Math.max(0, x), y: Math.max(0, y) })
    save.markDirty()
  }

  const handleComponentResize = async (componentId: string, width: number, height: number) => {
    updateComponent(componentId, { width, height })
    save.markDirty()
  }

  const handlePropertyChange = async (property: string, value: any) => {
    const selected = state.components.find((c) => c.id === state.selectedComponentId)
    if (!selected) return

    // Layers are changed by the server, which swaps with any component already on that layer
    if (property === 'zIndex') {
      const newZIndex = Math.round(Number(value))
      if (!Number.isFinite(newZIndex) || newZIndex < 0 || newZIndex > 100 || newZIndex === selected.zIndex) return
      try {
        await save.save() // pending edits are saved first so reloading the banner cannot lose them
        await layerService.reorderComponent(id, selected.id, newZIndex)
        await load()
        selectComponent(selected.id)
      } catch (error) {
        toast.error(error instanceof Error && !(error as any).response ? error.message : getErrorMessage(error, 'Could not change the layer'))
      }
      return
    }

    if (CONTENT_PROPERTIES.has(property)) {
      const next = { ...(selected.data as object), [property]: value } as Record<string, unknown>
      if (property === 'mediaUrl') next.mediaFileId = '' // an address typed in replaces any uploaded file
      updateComponent(selected.id, { data: next as unknown as typeof selected.data })
    } else {
      updateComponent(selected.id, { [property]: value })
    }
    save.markDirty()
  }

  /** Uploads the chosen file and stores it in the selected image or video component. */
  const handleUploadMedia = async (file: File) => {
    const selected = state.components.find((c) => c.id === state.selectedComponentId)
    if (!selected || (selected.type !== 'image' && selected.type !== 'video')) return

    const result = await media.upload(file, selected.type)
    if (!result) return

    updateComponent(selected.id, {
      data: {
        ...(selected.data as object),
        mediaFileId: result.mediaFileId,
        mediaUrl: result.url,
        ...(selected.type === 'image' ? { alt: (selected.data as any).alt && (selected.data as any).alt !== 'Image' ? (selected.data as any).alt : file.name } : {}),
      } as typeof selected.data,
    })
    save.markDirty()
    toast.success(`${file.name} uploaded`)
  }

  const handleDeleteComponent = async () => {
    if (!state.selectedComponentId || !state.banner) return

    try {
      await bannerService.deleteComponent(state.banner.id, state.selectedComponentId)
      deleteComponent(state.selectedComponentId)
      selectComponent(null)
      toast.success('Component deleted')
    } catch (error) {
      toast.error(getErrorMessage(error, 'Failed to delete component'))
    }
  }

  const handleSave = async () => {
    try {
      await save.save()
      toast.success('Banner saved')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to save banner')
    }
  }

  if (state.isLoading && !state.error) {
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
          <p role="alert" className="text-red-600 mb-4">Error: {state.error}</p>
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
        bannerId={id || 'unknown'}
        bannerName={state.banner?.title}
        backHref="/banners"
        onSave={handleSave}
        onUndo={() => {}}
        onRedo={() => {}}
        onTogglePreview={() => setPreviewMode(!state.isPreviewMode)}
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
          onUploadMedia={handleUploadMedia}
          uploadProgress={media.isUploading ? media.progress : null}
          uploadError={media.error}
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
        muted: true,
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
    <RequireAuth roles={[Roles.ShopOwner, Roles.SalesExecutive]}>
      <EditorProvider>
        <EditorContent />
      </EditorProvider>
    </RequireAuth>
  )
}
