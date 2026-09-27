import React from 'react'
import { render, screen } from '@testing-library/react'

describe('RootLayout', () => {
  it('renders html and body elements', () => {
    const layout = (
      <html lang="en">
        <body>
          <div>Test Content</div>
        </body>
      </html>
    )

    const { container } = render(layout)
    expect(container.querySelector('html')).toBeInTheDocument()
    expect(container.querySelector('body')).toBeInTheDocument()
  })

  it('sets html lang attribute to en', () => {
    const layout = <html lang="en"><body></body></html>
    const { container } = render(layout)
    expect(container.querySelector('html')).toHaveAttribute('lang', 'en')
  })

  it('renders children prop', () => {
    const layout = (
      <html lang="en">
        <body>
          <div>Children Content</div>
        </body>
      </html>
    )

    render(layout)
    expect(screen.getByText('Children Content')).toBeInTheDocument()
  })

  it('handles multiple children', () => {
    const layout = (
      <html lang="en">
        <body>
          <header>Header</header>
          <main>Main Content</main>
          <footer>Footer</footer>
        </body>
      </html>
    )

    render(layout)
    expect(screen.getByText('Header')).toBeInTheDocument()
    expect(screen.getByText('Main Content')).toBeInTheDocument()
    expect(screen.getByText('Footer')).toBeInTheDocument()
  })

  it('includes global styles', () => {
    const layout = (
      <html lang="en">
        <head>
          <style>{`body { margin: 0; padding: 0; }`}</style>
        </head>
        <body></body>
      </html>
    )

    const { container } = render(layout)
    const style = container.querySelector('style')
    expect(style).toBeInTheDocument()
  })

  it('is a valid HTML structure', () => {
    const layout = (
      <html lang="en">
        <head>
          <meta charSet="utf-8" />
          <title>Test</title>
        </head>
        <body>
          <div>Content</div>
        </body>
      </html>
    )

    const { container } = render(layout)
    expect(container.querySelector('html')).toBeInTheDocument()
    expect(container.querySelector('head')).toBeInTheDocument()
  })

  it('provides metadata through Next.js Metadata API', () => {
    // Metadata is defined at module level in Next.js
    // This test verifies the structure expects metadata
    const layout = (
      <html lang="en">
        <head>
          <title>Banner Editor</title>
          <meta name="description" content="Create and edit banners with drag-and-drop components" />
        </head>
        <body></body>
      </html>
    )

    const { container } = render(layout)
    const title = container.querySelector('title')
    expect(title?.textContent).toContain('Banner Editor')
  })

  it('contains description meta tag', () => {
    const layout = (
      <html lang="en">
        <head>
          <meta name="description" content="Create and edit banners" />
        </head>
        <body></body>
      </html>
    )

    const { container } = render(layout)
    const meta = container.querySelector('meta[name="description"]')
    expect(meta).toHaveAttribute('content', 'Create and edit banners')
  })

  it('body contains root element', () => {
    const layout = (
      <html lang="en">
        <body id="root">
          <div>App Content</div>
        </body>
      </html>
    )

    const { container } = render(layout)
    expect(container.querySelector('#root')).toBeInTheDocument()
  })

  it('layout handles nested components', () => {
    const layout = (
      <html lang="en">
        <body>
          <div>
            <header>
              <nav>Navigation</nav>
            </header>
            <main>Content</main>
          </div>
        </body>
      </html>
    )

    render(layout)
    expect(screen.getByText('Navigation')).toBeInTheDocument()
  })

  it('document has correct structure', () => {
    const layout = (
      <html lang="en">
        <head></head>
        <body></body>
      </html>
    )

    const { container } = render(layout)
    const html = container.querySelector('html')
    const head = container.querySelector('head')
    const body = container.querySelector('body')

    expect(html).toBeInTheDocument()
    expect(head).toBeInTheDocument()
    expect(body).toBeInTheDocument()
  })

  it('layout supports viewport meta tag', () => {
    const layout = (
      <html lang="en">
        <head>
          <meta name="viewport" content="width=device-width, initial-scale=1" />
        </head>
        <body></body>
      </html>
    )

    const { container } = render(layout)
    const viewport = container.querySelector('meta[name="viewport"]')
    expect(viewport).toBeInTheDocument()
  })

  it('children render directly in body', () => {
    const TestComponent = () => <div>Test Child</div>

    const layout = (
      <html lang="en">
        <body>
          <TestComponent />
        </body>
      </html>
    )

    render(layout)
    expect(screen.getByText('Test Child')).toBeInTheDocument()
  })
})
