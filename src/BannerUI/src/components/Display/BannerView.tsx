'use client'

import React, { useEffect, useRef, useState } from 'react'
import { Banner, BannerComponent } from '@/types/banner'

interface BannerViewProps {
  banner: Banner
}

/** Plays a banner full-screen: every component in place, scaled to fit the screen. */
export function BannerView({ banner }: BannerViewProps) {
  const holder = useRef<HTMLDivElement>(null)
  const [scale, setScale] = useState(1)

  useEffect(() => {
    const fit = () => {
      const box = holder.current?.getBoundingClientRect()
      if (!box || box.width === 0 || box.height === 0) return
      setScale(Math.min(box.width / banner.width, box.height / banner.height))
    }
    fit()
    window.addEventListener('resize', fit)
    return () => window.removeEventListener('resize', fit)
  }, [banner.width, banner.height])

  return (
    <div ref={holder} className="flex h-full w-full items-center justify-center bg-black">
      <div
        data-testid="banner-stage"
        style={{
          position: 'relative',
          width: banner.width,
          height: banner.height,
          transform: `scale(${scale})`,
          transformOrigin: 'center center',
          backgroundColor: banner.backgroundColor || '#ffffff',
          overflow: 'hidden',
          flex: 'none',
        }}
      >
        {[...banner.components]
          .filter((component) => component.isVisible)
          .sort((a, b) => a.zIndex - b.zIndex)
          .map((component) => (
            <ComponentView key={component.id} component={component} />
          ))}
      </div>
    </div>
  )
}

function ComponentView({ component }: { component: BannerComponent }) {
  const data = component.data as unknown as Record<string, any>

  return (
    <div
      data-component-id={component.id}
      style={{
        position: 'absolute',
        left: component.x,
        top: component.y,
        width: component.width,
        height: component.height,
        zIndex: component.zIndex,
        opacity: component.opacity,
        transform: `rotate(${component.rotation}deg)`,
      }}
    >
      {component.type === 'text' && (
        <div
          style={{
            width: '100%',
            height: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: data.textAlign === 'center' ? 'center' : data.textAlign === 'right' ? 'flex-end' : 'flex-start',
            color: data.color,
            fontSize: `${data.fontSize}px`,
            fontFamily: data.fontFamily,
            fontWeight: data.fontWeight,
            lineHeight: data.lineHeight,
            whiteSpace: 'pre-wrap',
            wordBreak: 'break-word',
          }}
        >
          {data.content}
        </div>
      )}
      {component.type === 'image' && data.mediaUrl && (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={data.mediaUrl} alt={data.alt ?? ''} style={{ width: '100%', height: '100%', objectFit: data.objectFit ?? 'cover' }} />
      )}
      {component.type === 'video' && data.mediaUrl && (
        <video
          src={data.mediaUrl}
          muted={data.muted !== false}
          loop={data.loop !== false}
          autoPlay
          playsInline
          style={{ width: '100%', height: '100%', objectFit: 'cover' }}
        />
      )}
      {component.type === 'graphics' && (
        <div
          style={{
            width: '100%',
            height: '100%',
            backgroundColor: data.fillColor,
            borderStyle: 'solid',
            borderColor: data.strokeColor,
            borderWidth: data.strokeWidth,
            borderRadius: data.shapeType === 'circle' ? '50%' : undefined,
          }}
        />
      )}
    </div>
  )
}
