# Banner Editor UI

Modern React-based banner editor with drag-drop components, real-time preview, and version control integration.

## Quick Start

### Prerequisites
- Node.js 18+ 
- npm or yarn

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

### Build

```bash
npm run build
npm start
```

### Testing

```bash
npm test
npm run test:watch
npm run test:coverage
```

## Architecture

- **Framework**: Next.js 14 with App Router
- **Language**: TypeScript (strict mode)
- **State**: React Context + Custom Hooks
- **Styling**: Tailwind CSS + CSS Modules
- **Drag-Drop**: react-beautiful-dnd
- **Testing**: Jest + React Testing Library

## Project Structure

```
src/
├── app/                 # Next.js App Router
├── components/          # React components
├── context/            # Global state (EditorContext)
├── hooks/              # Custom React hooks
├── api/                # API client layer
├── types/              # TypeScript types
├── styles/             # Global styles
└── utils/              # Helper functions
```

## Key Features

✅ Drag-and-drop canvas  
✅ Multiple component types (Text, Image, Video, Graphics)  
✅ Property editing  
✅ Z-index management  
✅ Auto-save (30s interval)  
✅ Undo/Redo  
✅ Version history  
✅ Media upload with chunking  
✅ Visual effects  
✅ Preview mode  
✅ Zoom controls  

## API Configuration

Set `NEXT_PUBLIC_API_URL` in `.env.local`:

```
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

## Development

### Adding Components

1. Create component in `src/components/`
2. Add TypeScript interfaces in `src/types/`
3. Create tests in `__tests__/`
4. Export from `src/components/index.ts`

### Adding Hooks

1. Create hook in `src/hooks/`
2. Create tests in `__tests__/hooks/`
3. Document with JSDoc comments

### API Integration

Use the service layer in `src/api/`:

```typescript
import { bannerService } from '@/api/bannerService'

const banner = await bannerService.getBanner(id)
```

## Performance

- Canvas rendering: < 2s initial load
- Component drag-drop: 60 FPS
- Property updates: 300ms debounce
- API responses: < 1s target
- Bundle size: < 500KB gzipped

## Accessibility

- WCAG 2.1 Level AA compliance
- Keyboard navigation support
- Screen reader friendly
- Proper color contrast
- ARIA labels on interactive elements

## Contributing

1. Create feature branch
2. Make changes with tests
3. Ensure linting passes
4. Submit PR

## License

MIT
