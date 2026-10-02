import React from 'react'
import { render, screen } from '@testing-library/react'
import { BannerView } from '@/components/Display/BannerView'
import { DefaultBoard } from '@/components/Display/DefaultBoard'
import { Banner, BannerComponent } from '@/types/banner'

const component = (overrides: Partial<BannerComponent>): BannerComponent => ({
  id: 'c', bannerId: 'b', type: 'text', x: 10, y: 20, width: 200, height: 50, zIndex: 0, rotation: 0, opacity: 1,
  isVisible: true, data: { content: 'Hello' } as never, effects: [], createdAt: '', updatedAt: '', ...overrides,
})

const banner = (components: BannerComponent[]): Banner => ({
  id: 'b', shopId: 's', title: 'Sale', description: '', width: 1200, height: 600, backgroundColor: '#fff',
  components, createdAt: '', updatedAt: '', version: 1,
})

describe('BannerView', () => {
  it('draws visible components in layer order and skips hidden ones', () => {
    const { container } = render(
      <BannerView
        banner={banner([
          component({ id: 'top', zIndex: 5, data: { content: 'Front' } as never }),
          component({ id: 'hidden', zIndex: 3, isVisible: false, data: { content: 'Secret' } as never }),
          component({ id: 'back', zIndex: 1, data: { content: 'Back' } as never }),
        ])}
      />
    )

    const drawn = Array.from(container.querySelectorAll('[data-component-id]')).map((el) => el.getAttribute('data-component-id'))
    expect(drawn).toEqual(['back', 'top'])
    expect(screen.queryByText('Secret')).not.toBeInTheDocument()
  })

  it('places a component with its position, size, opacity and rotation', () => {
    const { container } = render(
      <BannerView banner={banner([component({ x: 30, y: 40, width: 100, height: 60, opacity: 0.5, rotation: 15 })])} />
    )

    const style = (container.querySelector('[data-component-id]') as HTMLElement).style
    expect(style.left).toBe('30px')
    expect(style.top).toBe('40px')
    expect(style.width).toBe('100px')
    expect(style.height).toBe('60px')
    expect(style.opacity).toBe('0.5')
    expect(style.transform).toBe('rotate(15deg)')
  })

  it('plays videos muted and looping by default, and shows images', () => {
    const { container } = render(
      <BannerView
        banner={banner([
          component({ id: 'v', type: 'video', data: { mediaUrl: 'http://x/v.mp4' } as never }),
          component({ id: 'i', type: 'image', data: { mediaUrl: 'http://x/i.png', alt: 'Logo' } as never }),
        ])}
      />
    )

    const video = container.querySelector('video') as HTMLVideoElement
    expect(video.muted).toBe(true)
    expect(video.loop).toBe(true)
    expect(screen.getByAltText('Logo')).toHaveAttribute('src', 'http://x/i.png')
  })

  it('draws nothing for a picture that has no link yet', () => {
    const { container } = render(<BannerView banner={banner([component({ type: 'image', data: { mediaUrl: '' } as never })])} />)

    expect(container.querySelector('img')).toBeNull()
  })
})

describe('DefaultBoard', () => {
  it('shows the shop name and a note about why it is showing', () => {
    render(<DefaultBoard shopName="Olive Mart" note="This shop plan has ended." />)

    expect(screen.getByText('Olive Mart')).toBeInTheDocument()
    expect(screen.getByText(/plan has ended/)).toBeInTheDocument()
    expect(screen.getByTestId('default-board')).toBeInTheDocument()
  })

  it('still works before the shop name is known', () => {
    render(<DefaultBoard shopName="" />)

    expect(screen.getByText('our shop')).toBeInTheDocument()
  })
})
